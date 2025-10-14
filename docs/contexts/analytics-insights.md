# Analytics & Insights Layer

## Overview

The analytics layer transforms raw research data into actionable insights through mixed-method integration. This system combines quantitative survey responses with qualitative themes from video diaries, interviews, and open-ended responses. The end deliverable is narrative-driven "insight stories" that blend statistics, participant quotes, and media clips into compelling presentations for brand stakeholders. Data flows through a multi-stage pipeline enabling real-time participation tracking, thematic analysis, sentiment clustering, and automated insight generation.

## Data Ingestion

### Event Sources

**Engagement Events**:
- Post created, edited, deleted
- Comment added, edited, deleted
- Reaction added (like, upvote, sentiment)
- Member joined, left community
- Content viewed (page views, time on page)
- Search queries and results clicked

**Research Events**:
- Survey started, completed, abandoned
- Poll vote submitted
- Question response submitted (with response time)
- Video diary uploaded (with duration, file size)
- Photo diary uploaded (with annotations)
- Collage submitted
- Interview scheduled, completed, no-show
- Focus group attended
- Task assigned, started, completed
- Consent granted for study
- Incentive points awarded, redeemed
- Quality score assigned by moderator

**System Events**:
- User login, logout
- Session duration
- Feature usage (which features are accessed)
- Error occurrences

### Ingestion Methods

**Real-Time Stream** (Azure Event Hubs):
```csharp
// Publish event from application
public async Task PublishEngagementEvent(EngagementEvent evt)
{
    var eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt)));
    eventData.Properties["eventType"] = evt.Type;
    eventData.Properties["tenantId"] = evt.TenantId;
    
    await _eventHubClient.SendAsync(eventData);
}
```

**Batch Export** (Scheduled Jobs):
- Nightly export of aggregated metrics
- Historical data backfill
- Cross-database ETL for consolidated views

**Change Data Capture** (SQL Server CDC):
- Track changes to operational tables
- Stream to Event Hub for processing
- Maintain audit trail

## Data Pipeline Architecture

### Bronze Layer (Raw Data)

**Storage**: Azure Data Lake Storage Gen2

**Purpose**: Immutable storage of raw events exactly as received

**Schema**: JSON files partitioned by date and event type
```
/raw/events/
  /year=2025/
    /month=10/
      /day=11/
        /hour=14/
          post-created-{timestamp}.json
          comment-added-{timestamp}.json
```

**Retention**: 2 years

### Silver Layer (Cleaned and Enriched)

**Storage**: Azure Data Lake Storage Gen2 (Parquet format)

**Purpose**: Validated, deduplicated, and enriched data

**Transformations**:
- Schema validation and type conversion
- Deduplication based on event ID
- Enrichment with reference data (user name, community name)
- Anonymization of PII for GDPR compliance
- Derived fields (session ID, time zones, geo-location)

**Schema**: Parquet files with optimized partitioning
```
/silver/engagement/
  /tenant_id={guid}/
    /year=2025/
      /month=10/
        engagement-data.parquet
```

**Retention**: 5 years

### Gold Layer (Business Aggregates)

**Storage**: Azure SQL Database (analytics schema) or Azure Synapse

**Purpose**: Pre-aggregated metrics optimized for reporting

**Tables**:
- `DailyEngagementMetrics`: Posts, comments, reactions by day
- `MemberActivitySummary`: Per-member engagement scores
- `CommunityHealthMetrics`: Vitality indicators by community
- `SurveyResponseAggregates`: Response rates and distributions
- `ContentPerformance`: Top posts by reach and engagement

**Update Frequency**: 
- Real-time: Critical dashboards (15-minute refresh)
- Scheduled: Daily, weekly, monthly aggregations

## Data Processing

### Azure Stream Analytics

**Real-Time Aggregations**:
```sql
-- Active users in last 15 minutes
SELECT
    TenantId,
    CommunityId,
    System.Timestamp() AS WindowEnd,
    COUNT(DISTINCT UserId) AS ActiveUsers
INTO
    ActiveUserOutput
FROM
    EventHubInput TIMESTAMP BY EventTime
WHERE
    EventType IN ('PostCreated', 'CommentAdded', 'ContentViewed')
GROUP BY
    TenantId,
    CommunityId,
    TumblingWindow(minute, 15)
```

**Anomaly Detection**:
```sql
-- Detect unusual engagement spikes
SELECT
    TenantId,
    CommunityId,
    System.Timestamp() AS EventTime,
    COUNT(*) AS EventCount,
    AnomalyDetection_SpikeAndDip(COUNT(*), 95, 120, 'spikesanddips') OVER (LIMIT DURATION(hour, 2)) AS AnomalyScore
INTO
    AnomalyOutput
FROM
    EventHubInput TIMESTAMP BY EventTime
GROUP BY
    TenantId,
    CommunityId,
    SlidingWindow(minute, 5)
```

### Azure Data Factory / Synapse Pipelines

**ETL Workflows**:

1. **Ingest**: Copy data from operational DB to Data Lake
2. **Transform**: Run Databricks notebooks or SQL scripts
3. **Load**: Insert into analytical data warehouse
4. **Validate**: Data quality checks
5. **Notify**: Alert on pipeline failures or data anomalies

**Example Pipeline**:
```json
{
  "name": "DailyEngagementAggregation",
  "activities": [
    {
      "name": "CopyOperationalData",
      "type": "Copy",
      "source": { "type": "AzureSqlSource" },
      "sink": { "type": "ParquetSink" }
    },
    {
      "name": "TransformData",
      "type": "DatabricksNotebook",
      "notebook": "/analytics/aggregate-engagement"
    },
    {
      "name": "LoadToWarehouse",
      "type": "Copy",
      "source": { "type": "ParquetSource" },
      "sink": { "type": "AzureSqlSink" }
    }
  ],
  "triggers": [
    {
      "type": "ScheduleTrigger",
      "schedule": {
        "frequency": "Day",
        "interval": 1,
        "startTime": "2025-01-01T02:00:00Z"
      }
    }
  ]
}
```

### Databricks / Azure Synapse Spark

**Data Processing Jobs**:

**Engagement Score Calculation**:
```python
from pyspark.sql import SparkSession
from pyspark.sql.functions import col, sum, count, datediff, current_date

spark = SparkSession.builder.appName("EngagementScore").getOrCreate()

# Load silver layer data
posts = spark.read.parquet("/silver/engagement/posts")
comments = spark.read.parquet("/silver/engagement/comments")
reactions = spark.read.parquet("/silver/engagement/reactions")

# Calculate engagement score per user
user_engagement = (
    posts.groupBy("user_id", "tenant_id")
        .agg(count("*").alias("post_count"))
        .join(
            comments.groupBy("user_id", "tenant_id").agg(count("*").alias("comment_count")),
            ["user_id", "tenant_id"],
            "left"
        )
        .join(
            reactions.groupBy("user_id", "tenant_id").agg(count("*").alias("reaction_count")),
            ["user_id", "tenant_id"],
            "left"
        )
        .withColumn("engagement_score", 
            col("post_count") * 5 + 
            col("comment_count") * 3 + 
            col("reaction_count") * 1
        )
)

# Save to gold layer
user_engagement.write.mode("overwrite").parquet("/gold/engagement/user-scores")
```

**Member Segmentation (K-Means Clustering)**:
```python
from pyspark.ml.clustering import KMeans
from pyspark.ml.feature import VectorAssembler

# Prepare features
feature_cols = ["post_count", "comment_count", "reaction_count", "days_active", "avg_session_duration"]
assembler = VectorAssembler(inputCols=feature_cols, outputCol="features")
data_with_features = assembler.transform(user_engagement)

# Train clustering model
kmeans = KMeans(k=5, seed=42)
model = kmeans.fit(data_with_features)

# Assign segments
segmented_users = model.transform(data_with_features)
segmented_users = segmented_users.withColumnRenamed("prediction", "segment_id")

# Label segments
segment_labels = {
    0: "Lurkers",
    1: "Casual Participants",
    2: "Active Contributors",
    3: "Super Users",
    4: "At-Risk (Declining Activity)"
}

segmented_users.write.mode("overwrite").parquet("/gold/engagement/member-segments")
```

## AI/ML Integration

### Azure Cognitive Services

**Sentiment Analysis**:
```csharp
public async Task<SentimentResult> AnalyzeSentiment(string text, string tenantId)
{
    var credentials = new AzureKeyCredential(_cognitiveServicesKey);
    var client = new TextAnalyticsClient(new Uri(_cognitiveServicesEndpoint), credentials);
    
    var documents = new List<string> { text };
    var response = await client.AnalyzeSentimentBatchAsync(documents);
    
    var sentiment = response.Value[0].DocumentSentiment;
    
    return new SentimentResult
    {
        Sentiment = sentiment.Sentiment.ToString(),
        PositiveScore = sentiment.ConfidenceScores.Positive,
        NeutralScore = sentiment.ConfidenceScores.Neutral,
        NegativeScore = sentiment.ConfidenceScores.Negative
    };
}
```

**Key Phrase Extraction**:
```csharp
public async Task<List<string>> ExtractKeyPhrases(string text)
{
    var client = new TextAnalyticsClient(new Uri(_endpoint), new AzureKeyCredential(_key));
    var response = await client.ExtractKeyPhrasesAsync(text);
    
    return response.Value.ToList();
}
```

### Azure Machine Learning

**Custom Models**:

**Churn Prediction Model**:
- Features: Activity frequency, recency, content type preferences, session duration
- Algorithm: Gradient boosted trees (XGBoost)
- Output: Churn probability (0-1)
- Training: Weekly retraining on last 90 days of data
- Deployment: Real-time endpoint via Azure ML managed endpoint

**Content Recommendation Model**:
- Algorithm: Collaborative filtering (matrix factorization)
- Features: User-post interaction history, content categories, engagement patterns
- Output: Top N recommended posts for each user
- Update: Daily batch scoring

**Topic Modeling**:
- Algorithm: Latent Dirichlet Allocation (LDA)
- Input: Post content corpus
- Output: Topic distributions and top keywords per topic
- Use case: Automatic content categorization and trending topic detection

### Model Pipeline (MLOps)

```python
from azureml.core import Workspace, Dataset, Experiment
from azureml.train.sklearn import SKLearn
from azureml.pipeline.core import Pipeline, PipelineData
from azureml.pipeline.steps import PythonScriptStep

# Define workspace
ws = Workspace.from_config()

# Load training data
dataset = Dataset.get_by_name(ws, name='member-engagement-features')

# Define pipeline steps
train_step = PythonScriptStep(
    name="train_churn_model",
    script_name="train.py",
    arguments=["--data-path", dataset.as_named_input('training_data')],
    compute_target="compute-cluster",
    source_directory="./ml-scripts"
)

evaluate_step = PythonScriptStep(
    name="evaluate_model",
    script_name="evaluate.py",
    compute_target="compute-cluster",
    source_directory="./ml-scripts"
)

register_step = PythonScriptStep(
    name="register_model",
    script_name="register.py",
    compute_target="compute-cluster",
    source_directory="./ml-scripts"
)

# Create and run pipeline
pipeline = Pipeline(workspace=ws, steps=[train_step, evaluate_step, register_step])
experiment = Experiment(ws, 'churn-prediction-training')
run = experiment.submit(pipeline)
```

## Dashboards and Visualization

### Real-Time Operations Dashboard

**Power BI Streaming Dataset**:
- Active users (current 15 minutes)
- Posts per minute
- Error rate
- API response time

**Refresh**: Every 30 seconds via REST API push

### Executive Dashboard

**Metrics**:
- Total active users (DAU, MAU)
- Engagement rate (% of members active)
- Content creation rate (posts + comments per day)
- Survey response rate
- Member growth rate
- Retention cohorts

**Visualizations**:
- Time series charts for trends
- Cohort retention heatmap
- Geographic distribution map
- Top communities by engagement
- Member segment distribution

**Refresh**: Hourly

### Community Manager Dashboard

**Metrics** (per community):
- New members this week
- Post volume and trends
- Top contributors
- Content performance (most viewed, most engaged)
- Sentiment distribution
- Moderation queue size

**Actions**:
- Drill down to individual posts
- Export data to Excel
- Schedule reports via email

**Refresh**: Every 15 minutes

### Research Dashboard

**Survey Analytics**:
- Response rate by survey
- Completion time distribution
- Question-level responses (charts for choice questions, word clouds for text)
- Cross-tabulation (response by segment or demographic)
- Statistical significance testing

**Export Options**:
- Raw responses (CSV, Excel, SPSS)
- Charts and visualizations (PDF, PNG)
- Summary reports (Word, PDF)

### Custom Dashboards

**Tenant-Specific Views**:
- Admin users can create custom dashboards
- Drag-and-drop widget builder
- Filter by date range, community, segment
- Save and share with team members

**Widget Types**:
- KPI cards (single metric)
- Line/bar/pie charts
- Tables with sorting and filtering
- Heatmaps
- Word clouds

## Key Metrics and KPIs

### Engagement Metrics

**Daily Active Users (DAU)**:
```sql
SELECT 
    tenant_id,
    community_id,
    date,
    COUNT(DISTINCT user_id) AS dau
FROM engagement_events
WHERE date = CURRENT_DATE
GROUP BY tenant_id, community_id, date
```

**Engagement Rate**:
```sql
-- Percentage of members who engaged in last 30 days
SELECT 
    c.tenant_id,
    c.community_id,
    COUNT(DISTINCT m.user_id) AS total_members,
    COUNT(DISTINCT e.user_id) AS active_members,
    (COUNT(DISTINCT e.user_id) * 100.0 / NULLIF(COUNT(DISTINCT m.user_id), 0)) AS engagement_rate
FROM communities c
LEFT JOIN memberships m ON c.id = m.community_id
LEFT JOIN engagement_events e ON m.user_id = e.user_id 
    AND e.created_at >= DATEADD(day, -30, GETDATE())
GROUP BY c.tenant_id, c.community_id
```

**Content Velocity**:
```sql
-- Posts + comments per day
SELECT 
    date,
    SUM(post_count) AS posts,
    SUM(comment_count) AS comments,
    SUM(post_count + comment_count) AS total_content
FROM daily_content_metrics
WHERE date >= DATEADD(day, -30, GETDATE())
GROUP BY date
ORDER BY date
```

### Retention Metrics

**Cohort Retention**:
```sql
-- Week-over-week retention by signup cohort
WITH cohorts AS (
    SELECT 
        user_id,
        DATEPART(year, created_at) AS cohort_year,
        DATEPART(week, created_at) AS cohort_week
    FROM users
),
activity AS (
    SELECT DISTINCT
        e.user_id,
        DATEPART(year, e.created_at) AS activity_year,
        DATEPART(week, e.created_at) AS activity_week
    FROM engagement_events e
)
SELECT 
    c.cohort_year,
    c.cohort_week,
    a.activity_week - c.cohort_week AS weeks_since_signup,
    COUNT(DISTINCT a.user_id) AS retained_users,
    COUNT(DISTINCT c.user_id) AS cohort_size,
    (COUNT(DISTINCT a.user_id) * 100.0 / COUNT(DISTINCT c.user_id)) AS retention_rate
FROM cohorts c
LEFT JOIN activity a ON c.user_id = a.user_id
GROUP BY c.cohort_year, c.cohort_week, a.activity_week - c.cohort_week
ORDER BY c.cohort_year, c.cohort_week, weeks_since_signup
```

**Churn Analysis**:
```sql
-- Members with no activity in last 30 days
SELECT 
    user_id,
    MAX(event_date) AS last_active_date,
    DATEDIFF(day, MAX(event_date), GETDATE()) AS days_inactive
FROM engagement_events
GROUP BY user_id
HAVING DATEDIFF(day, MAX(event_date), GETDATE()) > 30
```

### Research Metrics

**Survey Response Rate**:
```sql
SELECT 
    s.id AS survey_id,
    s.title,
    COUNT(DISTINCT sv.user_id) AS invitations_sent,
    COUNT(DISTINCT sr.user_id) AS responses_completed,
    (COUNT(DISTINCT sr.user_id) * 100.0 / NULLIF(COUNT(DISTINCT sv.user_id), 0)) AS response_rate
FROM surveys s
LEFT JOIN survey_invitations sv ON s.id = sv.survey_id
LEFT JOIN survey_responses sr ON s.id = sr.survey_id AND sr.status = 'completed'
GROUP BY s.id, s.title
```

**Average Completion Time**:
```sql
SELECT 
    survey_id,
    AVG(DATEDIFF(second, started_at, completed_at)) AS avg_completion_seconds,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY DATEDIFF(second, started_at, completed_at)) AS median_completion_seconds
FROM survey_responses
WHERE status = 'completed'
GROUP BY survey_id
```

### Content Performance

**Top Posts by Engagement**:
```sql
SELECT 
    p.id,
    p.title,
    p.created_at,
    COUNT(DISTINCT c.id) AS comment_count,
    COUNT(DISTINCT r.id) AS reaction_count,
    COUNT(DISTINCT v.user_id) AS view_count,
    (COUNT(DISTINCT c.id) * 3 + COUNT(DISTINCT r.id) * 1) AS engagement_score
FROM posts p
LEFT JOIN comments c ON p.id = c.post_id
LEFT JOIN reactions r ON p.id = r.post_id
LEFT JOIN post_views v ON p.id = v.post_id
WHERE p.created_at >= DATEADD(day, -7, GETDATE())
GROUP BY p.id, p.title, p.created_at
ORDER BY engagement_score DESC
```

## Export and Integration APIs

### Data Export Endpoints

**Export Engagement Data**:
```csharp
[HttpGet("api/v1/analytics/engagement/export")]
[Authorize(Policy = "TenantAdmin")]
public async Task<IActionResult> ExportEngagementData(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] string format = "csv")
{
    var data = await _analyticsService.GetEngagementData(
        User.TenantId(), 
        startDate, 
        endDate
    );
    
    return format.ToLower() switch
    {
        "csv" => File(ToCsv(data), "text/csv", "engagement-data.csv"),
        "excel" => File(ToExcel(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "engagement-data.xlsx"),
        "json" => Ok(data),
        _ => BadRequest("Unsupported format")
    };
}
```

**Survey Results Export**:
```csharp
[HttpGet("api/v1/surveys/{surveyId}/results/export")]
[Authorize(Policy = "TenantAdmin")]
public async Task<IActionResult> ExportSurveyResults(
    Guid surveyId,
    [FromQuery] string format = "csv",
    [FromQuery] bool includeOpenEnded = true)
{
    var results = await _surveyService.GetSurveyResults(surveyId, includeOpenEnded);
    
    // Export in requested format
    var fileBytes = format.ToLower() switch
    {
        "csv" => _exportService.ToCsv(results),
        "excel" => _exportService.ToExcel(results),
        "spss" => _exportService.ToSpss(results),
        _ => throw new ArgumentException("Unsupported format")
    };
    
    return File(fileBytes, GetContentType(format), $"survey-{surveyId}-results.{format}");
}
```

### Integration with External BI Tools

**Power BI Direct Query**:
- SQL Database with read-only user
- Views optimized for BI queries
- Row-level security for tenant isolation

**Tableau Web Data Connector**:
- REST API endpoints returning JSON
- OAuth authentication
- Pagination for large datasets

**Looker / Metabase**:
- Direct database connection or REST API
- Pre-defined data models and relationships
- Custom SQL allowed for advanced users

### Webhook Event Streaming

**Configuration**:
```json
{
  "webhookUrl": "https://client-system.com/webhooks/engagement-events",
  "events": [
    "post.created",
    "survey.completed",
    "member.joined"
  ],
  "authentication": {
    "type": "bearer",
    "token": "encrypted-token"
  },
  "retryPolicy": {
    "maxRetries": 3,
    "backoffMultiplier": 2
  }
}
```

**Payload**:
```json
{
  "eventId": "uuid",
  "eventType": "post.created",
  "timestamp": "2025-10-11T14:30:00Z",
  "tenantId": "tenant-uuid",
  "data": {
    "postId": "post-uuid",
    "communityId": "community-uuid",
    "userId": "user-uuid",
    "title": "Post title",
    "contentPreview": "First 100 characters..."
  }
}
```

## Mixed-Method Reporting

### Quantitative + Qualitative Integration

**Philosophy**: True insights come from blending what participants say (qual) with how many say it (quant).

**Integration Patterns**:
1. **Quant First**: Survey reveals 65% dissatisfied with pricing → Dive into video diaries to understand why
2. **Qual First**: Interviews surface "confusing checkout" theme → Survey quantifies % affected
3. **Parallel**: Survey + video diaries run simultaneously, analyzed together in insight story

### Theme-Based Segmentation

```sql
-- Cross-tabulate survey responses by qualitative themes
SELECT 
    t.theme_name,
    sr.question_id,
    sr.answer,
    COUNT(*) as response_count,
    (COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (PARTITION BY sr.question_id)) as percentage
FROM survey_responses sr
INNER JOIN coded_segments cs ON sr.user_id = cs.user_id
INNER JOIN themes t ON cs.theme_id = t.id
WHERE sr.survey_id = @survey_id
GROUP BY t.theme_name, sr.question_id, sr.answer
ORDER BY t.theme_name, sr.question_id;
```

**Example Output**:

| Theme | Question | Answer | Count | % |
|-------|----------|--------|-------|---|
| Price Sensitive | Satisfaction | Very Dissatisfied | 45 | 68% |
| Price Sensitive | Satisfaction | Dissatisfied | 18 | 27% |
| Brand Loyal | Satisfaction | Satisfied | 32 | 61% |
| Brand Loyal | Satisfaction | Very Satisfied | 15 | 29% |

### Sentiment-Enhanced Statistics

**Enrich Quantitative Data with Qualitative Sentiment**:
- Survey shows 70% neutral on new feature
- Sentiment analysis of open-ends reveals 80% of "neutral" are actually negative
- Refined insight: "Participants are lukewarm-to-negative, not genuinely neutral"

**Implementation**:
```python
from azure.ai.textanalytics import TextAnalyticsClient

def analyze_open_ends(survey_id):
    open_responses = get_open_ended_responses(survey_id)
    
    for response in open_responses:
        sentiment = text_analytics_client.analyze_sentiment([response.text])[0]
        
        # Update response with sentiment scores
        update_response_sentiment(
            response.id,
            sentiment.sentiment,  # positive, neutral, negative, mixed
            sentiment.confidence_scores.positive,
            sentiment.confidence_scores.neutral,
            sentiment.confidence_scores.negative
        )
```

## Qualitative Coding & Themes

### Coding Workflow

**1. Data Preparation**:
- Transcribe video/audio (auto or manual)
- Import survey open-ends
- Segment media into codable clips

**2. Theme Development**:
- Analysts create initial coding taxonomy
- Read through responses to identify patterns
- Iteratively refine themes as coding progresses

**3. Collaborative Coding**:
```csharp
public class CodingService
{
    public async Task ApplyCode(Guid responseId, Guid codeId, Guid analystId)
    {
        var codedSegment = new CodedSegment
        {
            ResponseId = responseId,
            CodeId = codeId,
            AnalystId = analystId,
            CodedAt = DateTime.UtcNow
        };
        
        await _codedSegmentRepository.Create(codedSegment);
        
        // Publish event for real-time collaboration
        await _eventBus.Publish(new SegmentCoded 
        {
            ResponseId = responseId,
            CodeId = codeId,
            AnalystId = analystId
        });
    }
}
```

**4. Inter-Coder Reliability**:
- 10-20% of responses coded by multiple analysts
- Calculate Cohen's Kappa or Krippendorff's Alpha
- Resolve disagreements through discussion
- Target reliability: > 0.75

### AI-Assisted Theme Clustering

**Automated Theme Suggestions**:
```python
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.cluster import KMeans

def suggest_themes(responses, n_clusters=5):
    # Vectorize text responses
    vectorizer = TfidfVectorizer(max_features=100, stop_words='english')
    X = vectorizer.fit_transform([r.text for r in responses])
    
    # Cluster responses
    kmeans = KMeans(n_clusters=n_clusters, random_state=42)
    clusters = kmeans.fit_predict(X)
    
    # Extract top terms per cluster as theme suggestions
    theme_suggestions = []
    for i in range(n_clusters):
        cluster_center = kmeans.cluster_centers_[i]
        top_terms = [vectorizer.get_feature_names_out()[idx] 
                     for idx in cluster_center.argsort()[-10:]]
        theme_suggestions.append({
            'cluster_id': i,
            'suggested_name': ' '.join(top_terms[:3]),
            'top_terms': top_terms,
            'response_count': sum(clusters == i)
        })
    
    return theme_suggestions
```

**Azure Cognitive Services Integration**:
- Key phrase extraction for theme suggestions
- Entity recognition (brands, products mentioned)
- Sentiment analysis at sentence level

### Quote Management

**Quote Selection Criteria**:
- Highly illustrative of theme
- Authentic voice
- Concise and punchy
- Representative (not outlier unless noted)

**Quote Database**:
```sql
CREATE TABLE quotes (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    response_id UNIQUEIDENTIFIER,
    theme_id UNIQUEIDENTIFIER,
    quote_text NVARCHAR(MAX),
    context NVARCHAR(500), -- What question or task
    attribution VARCHAR(100), -- "Female, 35-44" or "Sarah, New York"
    is_anonymized BIT,
    impact_score INT, -- 1-5, how powerful is this quote
    selected_for_story_id UNIQUEIDENTIFIER NULL,
    created_at DATETIME2
);
```

## Insight Story Builder

### Story Structure

**Standard Sections**:
1. **Executive Summary**: Key findings in 2-3 bullet points
2. **Research Objectives**: What we wanted to learn
3. **Methodology**: Sample size, activities conducted, timeline
4. **Key Findings**: 3-5 major themes with evidence
5. **Supporting Details**: Deep dives into each theme
6. **Recommendations**: Actionable next steps
7. **Appendix**: Full data tables, additional quotes

### Story Compilation Workflow

**1. Select Insights**:
```typescript
interface InsightStorySection {
  id: string;
  title: string;
  type: 'text' | 'quote' | 'chart' | 'media' | 'mixed';
  content: {
    text?: string;
    quotes?: Array<{
      quote: string;
      attribution: string;
      themeId: string;
    }>;
    chartData?: ChartConfig;
    mediaUrls?: string[];
  };
  order: number;
}
```

**2. Drag-and-Drop Interface**:
- Analysts drag quotes, charts, media clips into story canvas
- Real-time preview of how story will look
- Templates for common story formats

**3. Narrative Generation** (AI-Assisted):
```csharp
public async Task<string> GenerateNarrativeFromTheme(Guid themeId)
{
    var theme = await _themeRepository.GetById(themeId);
    var codes = await _codeRepository.GetByTheme(themeId);
    var quotes = await _quoteRepository.GetByTheme(themeId);
    
    var prompt = $@"
        Generate a 2-3 paragraph narrative insight based on the following qualitative research theme:
        
        Theme: {theme.Name}
        Description: {theme.Description}
        Sub-themes: {string.Join(", ", codes.Select(c => c.Name))}
        Sample quotes:
        {string.Join("\n", quotes.Take(5).Select(q => $"- \"{q.Text}\" ({q.Attribution})"))}
        
        The narrative should synthesize the key insights from these quotes in clear, compelling language
        suitable for a client presentation.
    ";
    
    var narrative = await _azureOpenAI.GenerateCompletion(prompt);
    return narrative;
}
```

**4. Export Formats**:

**PowerPoint**:
```csharp
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

public async Task<byte[]> ExportToPowerPoint(InsightStory story)
{
    using var memoryStream = new MemoryStream();
    using var presentation = PresentationDocument.Create(memoryStream, PresentationDocumentType.Presentation);
    
    var presentationPart = presentation.AddPresentationPart();
    presentationPart.Presentation = new Presentation();
    
    // Apply brand theme
    var themePart = presentationPart.AddNewPart<ThemePart>();
    themePart.Theme = LoadBrandTheme(story.TenantId);
    
    // Generate slides
    foreach (var section in story.Sections.OrderBy(s => s.Order))
    {
        var slide = CreateSlideFromSection(section, presentationPart);
        presentationPart.Presentation.AppendChild(slide);
    }
    
    presentation.Save();
    return memoryStream.ToArray();
}
```

**PDF with Embedded Video**:
- Generate PDF with QR codes linking to video clips
- Or embed video thumbnails with click-to-play links

**Video Reel**:
- Stitch together selected video clips with transitions
- Add text overlays for theme names and key quotes
- Background music (optional, client-selectable)

### Real-Time Collaboration

**Multiple Analysts Editing**:
- WebSocket-based real-time updates
- Operational Transform for conflict resolution
- User presence indicators ("Jane is editing Section 3")
- Comment threads for peer review

### Version Control

**Story Versions**:
- Draft → Review → Approved → Published
- Track changes between versions
- Rollback capability
- Export history (who exported when)

## Research-Specific Dashboards

### Participation Dashboard

**Metrics**:
- Task completion rate by activity type
- Time to complete tasks
- Response quality scores
- Dropout points (where participants abandon)
- Incentive redemption rate

**Visualization**:
```typescript
const ParticipationFunnel = () => {
  const data = [
    { stage: 'Invited', count: 500 },
    { stage: 'Consented', count: 420 },
    { stage: 'Started Task', count: 380 },
    { stage: 'Submitted', count: 310 },
    { stage: 'Approved', count: 295 }
  ];
  
  return <FunnelChart data={data} colors={['#4CAF50', '#2196F3', '#FF9800', '#F44336']} />;
};
```

### Moderator Performance Dashboard

**Metrics**:
- Review queue size and age
- Average review time per submission
- Approval vs. rejection rate
- Follow-up question frequency
- Participant satisfaction with moderation

### Response Quality Dashboard

**Metrics**:
- Average response length (text responses)
- Video duration distribution
- Photos with annotations percentage
- Open-ended depth score (AI-assessed)
- Richness of detail (thin → moderate → rich)

## Data Privacy and Compliance

### GDPR Compliance

**Right to Access**:
- API endpoint to retrieve all data for a user
- Includes posts, comments, survey responses, profile data
- Export in machine-readable format (JSON)

**Right to Erasure**:
- Soft delete user account (mark as deleted)
- Anonymize user-generated content (replace user ID with "deleted user")
- Delete PII but retain anonymized analytics data

**Data Minimization**:
- Only collect necessary fields for analytics
- Automatic expiration of detailed logs (2 years)
- Aggregated data retained longer than raw events

### Tenant Data Isolation

**Query Filters**:
```sql
-- All analytics queries include tenant filter
SELECT * FROM engagement_metrics
WHERE tenant_id = @current_tenant_id
```

**Row-Level Security**:
```sql
CREATE SECURITY POLICY TenantIsolationPolicy
ADD FILTER PREDICATE dbo.fn_tenantAccessPredicate(tenant_id)
ON dbo.engagement_metrics,
ON dbo.survey_results
WITH (STATE = ON);
```

**Access Auditing**:
- Log all analytics queries with user and tenant
- Alert on cross-tenant access attempts
- Regular audit reviews

## Performance Optimization

**Strategies**:
- Materialized views for common aggregations
- Columnstore indexes for analytical queries
- Partitioning by tenant and date
- Caching of dashboard data (15-minute TTL)
- Pre-aggregated metrics tables
- Query result caching in Redis
- Asynchronous export jobs for large datasets

