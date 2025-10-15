# Analytics & Insights

## Data Pipeline

### Three-Layer Architecture
- **Bronze**: Raw event data in Data Lake (JSON)
- **Silver**: Cleaned and enriched data (Parquet)  
- **Gold**: Aggregated business metrics for dashboards

### Event Sources
**Engagement**: Post creation, comments, reactions, member activity  
**Research**: Survey responses, task completions, interview data  
**System**: User sessions, feature usage, errors

### Real-Time Processing
Azure Stream Analytics for live dashboards and anomaly detection.

## Key Metrics

### Engagement KPIs
- Daily/Monthly Active Users (DAU/MAU)
- Engagement rate (% of members active)
- Content velocity (posts + comments per day)
- Member retention and churn analysis

### Research Analytics  
- Survey response rates and completion times
- Task completion funnel analysis
- Quality scores and moderation metrics
- Participant segmentation and behavior patterns

## Mixed-Method Integration

### Quantitative + Qualitative
- Cross-tabulate survey responses by qualitative themes
- Sentiment analysis on open-ended responses
- Theme-based participant segmentation
- Narrative insight stories combining stats with quotes

### Insight Story Builder
- Drag-and-drop interface for compiling research deliverables
- Export to PowerPoint, PDF, or video formats
- Real-time collaboration for multiple analysts

## AI/ML Features

### Azure Cognitive Services
- **Speech-to-Text**: Auto-transcription of video diaries and interviews
- **Sentiment Analysis**: Automated sentiment scoring on text responses
- **Key Phrase Extraction**: Theme identification assistance

### Custom Models
- **Churn Prediction**: ML model to identify at-risk participants
- **Content Recommendation**: Collaborative filtering for engagement
- **Topic Modeling**: Automatic content categorization

## Reporting & Dashboards

### Executive Dashboards
- High-level KPIs and trends
- Cohort retention analysis
- Geographic and demographic breakdowns

### Research Dashboards  
- Survey completion tracking
- Response quality metrics
- Export capabilities for external analysis tools
