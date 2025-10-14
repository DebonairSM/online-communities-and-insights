# Integration & Extensibility

## External System Integration Strategy

### Research Tool Integration

**Supported Research Platforms**:
- **Qualtrics**: Survey platform for advanced quantitative research
- **SPSS / SAS**: Statistical analysis software
- **MaxQDA / NVivo / ATLAS.ti**: Qualitative data analysis (QDA) software
- **Confirmit / Decipher**: Survey programming platforms
- **UserTesting / dScout**: Complementary research tools

**Qualtrics Integration**:
```csharp
public class QualtricsIntegrationService
{
    public async Task ExportSurveyToQualtrics(Guid surveyId)
    {
        var survey = await _surveyRepository.GetById(surveyId);
        var questions = await _questionRepository.GetBySurvey(surveyId);
        
        // Convert to Qualtrics QSF format
        var qsfExport = new
        {
            SurveyEntry = new
            {
                SurveyID = survey.Id.ToString(),
                SurveyName = survey.Title,
                SurveyDescription = survey.Description,
                SurveyOwnerID = survey.CreatorId.ToString(),
                SurveyLanguage = "EN",
                SurveyActiveResponseSet = "Default",
                SurveyStatus = "Active"
            },
            Questions = questions.Select(q => new
            {
                QuestionID = q.Id.ToString(),
                QuestionType = MapToQualtricsType(q.QuestionType),
                Selector = GetQualtricsSelector(q.QuestionType),
                QuestionText = q.QuestionText,
                Choices = q.Options?.Select((opt, idx) => new
                {
                    ChoiceID = (idx + 1).ToString(),
                    Display = opt
                }).ToList(),
                Validation = MapValidation(q.Validation)
            })
        };
        
        var json = JsonSerializer.Serialize(qsfExport, new JsonSerializerOptions { WriteIndented = true });
        return json;
    }
    
    public async Task ImportResponsesFromQualtrics(string qualtricsApiKey, string surveyId)
    {
        // Fetch responses from Qualtrics API
        var responses = await _qualtricsClient.GetSurveyResponses(qualtricsApiKey, surveyId);
        
        // Import into platform
        foreach (var response in responses)
        {
            await _surveyResponseService.ImportResponse(response);
        }
    }
}
```

**SPSS Export**:
```csharp
public class SpssExportService
{
    public async Task<byte[]> ExportToSpss(Guid surveyId)
    {
        var responses = await _surveyResponseRepository.GetBySurvey(surveyId);
        var questions = await _questionRepository.GetBySurvey(surveyId);
        
        // Create SPSS .sav file using StatTag or similar library
        using var spssDataDocument = new SpssDataDocument();
        
        // Define variables
        foreach (var question in questions)
        {
            var variable = new SpssNumericVariable
            {
                Name = $"Q{question.Order}",
                Label = question.QuestionText,
                MeasurementLevel = GetMeasurementLevel(question.QuestionType)
            };
            
            // Add value labels for choice questions
            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                foreach (var (option, index) in question.Options.Select((o, i) => (o, i)))
                {
                    variable.ValueLabels.Add(index + 1, option);
                }
            }
            
            spssDataDocument.Variables.Add(variable);
        }
        
        // Add response data
        foreach (var response in responses)
        {
            var caseData = spssDataDocument.Cases.New();
            // Populate case data from response
            await PopulateCaseData(caseData, response);
        }
        
        // Save to byte array
        using var memoryStream = new MemoryStream();
        spssDataDocument.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }
}
```

**MaxQDA / NVivo Export**:
```csharp
public class QualitativeDataExportService
{
    public async Task<string> ExportToMaxQda(Guid studyId)
    {
        var responses = await _responseRepository.GetQualitativeResponses(studyId);
        var themes = await _themeRepository.GetByStudy(studyId);
        var codedSegments = await _codedSegmentRepository.GetByStudy(studyId);
        
        // Create MaxQDA project XML
        var maxQdaProject = new XElement("MaxQDAProject",
            new XAttribute("version", "2024"),
            new XElement("CodeSystem",
                themes.Select(theme => new XElement("Code",
                    new XAttribute("guid", theme.Id),
                    new XAttribute("name", theme.Name),
                    new XAttribute("color", theme.Color),
                    theme.SubThemes?.Select(sub => new XElement("Code",
                        new XAttribute("guid", sub.Id),
                        new XAttribute("name", sub.Name)
                    ))
                ))
            ),
            new XElement("Documents",
                responses.Select(r => new XElement("Document",
                    new XAttribute("guid", r.Id),
                    new XAttribute("name", $"Participant_{r.ParticipantId}"),
                    new XElement("Text", r.ResponseText),
                    new XElement("Codes",
                        codedSegments.Where(cs => cs.ResponseId == r.Id)
                            .Select(cs => new XElement("CodedSegment",
                                new XAttribute("codeGuid", cs.CodeId),
                                new XAttribute("start", cs.StartPosition),
                                new XAttribute("end", cs.EndPosition)
                            ))
                    )
                ))
            )
        );
        
        return maxQdaProject.ToString();
    }
    
    public async Task<string> ExportToNVivo(Guid studyId)
    {
        // Similar to MaxQDA but in NVivo's .nvp format
        // NVivo uses a different XML schema
        // Implementation details omitted for brevity
    }
}
```

### CRM Integration

**Supported CRM Systems**:
- Salesforce Sales Cloud
- Microsoft Dynamics 365
- HubSpot CRM
- Zoho CRM

**Integration Capabilities**:
- Bidirectional participant sync (profile data, demographics)
- Research participation tracking (survey completion, diary submission)
- Engagement scoring for participant prioritization
- Campaign tracking (recruitment source attribution)

**Salesforce Integration Example**:
```csharp
public class SalesforceIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly string _instanceUrl;
    private readonly string _accessToken;
    
    public async Task SyncMemberToCrm(User user)
    {
        var contact = new
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Community_Member_ID__c = user.Id.ToString(),
            Engagement_Score__c = await _analyticsService.GetEngagementScore(user.Id),
            Last_Active_Date__c = user.LastLoginAt
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"{_instanceUrl}/services/data/v58.0/sobjects/Contact",
            contact
        );
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SalesforceCreateResponse>();
            user.ExternalCrmId = result.Id;
            await _userRepository.Update(user);
        }
    }
    
    public async Task LogActivityToCrm(Post post)
    {
        var user = await _userRepository.GetById(post.AuthorId);
        
        if (string.IsNullOrEmpty(user.ExternalCrmId))
        {
            return; // User not synced to CRM
        }
        
        var task = new
        {
            WhoId = user.ExternalCrmId,
            Subject = "Community Post Created",
            Description = $"Member posted: {post.Title}",
            ActivityDate = post.CreatedAt.ToString("yyyy-MM-dd"),
            Status = "Completed",
            Community_Post_URL__c = $"https://community.com/posts/{post.Id}"
        };
        
        await _httpClient.PostAsJsonAsync(
            $"{_instanceUrl}/services/data/v58.0/sobjects/Task",
            task
        );
    }
}
```

**Sync Configuration** (per tenant):
```json
{
  "crmProvider": "Salesforce",
  "credentials": {
    "instanceUrl": "https://company.my.salesforce.com",
    "clientId": "encrypted-client-id",
    "clientSecret": "encrypted-client-secret",
    "refreshToken": "encrypted-refresh-token"
  },
  "syncSettings": {
    "syncDirection": "bidirectional",
    "syncFrequency": "realtime",
    "mappings": {
      "User.FirstName": "Contact.FirstName",
      "User.LastName": "Contact.LastName",
      "User.Email": "Contact.Email",
      "User.CustomFields.companySize": "Contact.Company_Size__c"
    },
    "activityLogging": {
      "enabled": true,
      "events": ["post.created", "survey.completed"]
    }
  }
}
```

### Customer Data Platform (CDP) Integration

**Supported CDPs**:
- Segment
- mParticle
- Lytics
- Adobe Experience Platform

**Event Streaming**:
```csharp
public class SegmentIntegrationService
{
    public async Task TrackEvent(string userId, string eventName, object properties)
    {
        var payload = new
        {
            userId = userId,
            event = eventName,
            properties = properties,
            timestamp = DateTime.UtcNow,
            context = new
            {
                app = new { name = "Online Communities", version = "1.0" },
                library = new { name = ".NET", version = "8.0" }
            }
        };
        
        await _httpClient.PostAsJsonAsync("https://api.segment.io/v1/track", payload);
    }
    
    public async Task IdentifyUser(User user)
    {
        var traits = new
        {
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            createdAt = user.CreatedAt,
            engagementScore = await _analyticsService.GetEngagementScore(user.Id),
            segment = await _analyticsService.GetUserSegment(user.Id)
        };
        
        await _httpClient.PostAsJsonAsync("https://api.segment.io/v1/identify", new
        {
            userId = user.Id.ToString(),
            traits = traits,
            timestamp = DateTime.UtcNow
        });
    }
}

// Usage in domain event handlers
public class PostCreatedEventHandler
{
    public async Task Handle(PostCreated evt)
    {
        await _segmentService.TrackEvent(
            evt.AuthorId.ToString(),
            "Post Created",
            new
            {
                postId = evt.PostId,
                communityId = evt.CommunityId,
                contentType = "text",
                hasImage = evt.MediaUrls?.Any() ?? false
            }
        );
    }
}
```

**Identity Resolution**:
```csharp
public class IdentityResolutionService
{
    public async Task LinkIdentities(Guid userId, string externalId, string source)
    {
        var identity = new UserIdentity
        {
            UserId = userId,
            ExternalId = externalId,
            Source = source, // "CRM", "CDP", "Marketing Automation"
            CreatedAt = DateTime.UtcNow
        };
        
        await _identityRepository.Create(identity);
        
        // Notify CDP of identity link
        await _cdpService.Alias(userId.ToString(), externalId);
    }
}
```

### BI Tool Integration

**Power BI Integration**:

**Direct Query** (read-only database user):
```sql
-- Create read-only user for Power BI
CREATE USER powerbi_readonly WITH PASSWORD = 'SecurePassword123!';

-- Grant read access to analytics views
GRANT SELECT ON SCHEMA::analytics TO powerbi_readonly;

-- Row-level security for tenant isolation
CREATE FUNCTION dbo.fn_PowerBITenantFilter(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
AS
RETURN SELECT 1 AS AccessGranted
WHERE @TenantId IN (SELECT TenantId FROM dbo.TenantAccessList WHERE PowerBIUser = USER_NAME());
```

**REST API for Custom Visuals**:
```csharp
[HttpGet("api/v1/analytics/powerbi/engagement")]
[Authorize(Policy = "BIToolAccess")]
public async Task<IActionResult> GetEngagementData(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate)
{
    var tenantId = User.TenantId();
    
    var data = await _analyticsRepository.GetEngagementMetrics(
        tenantId, 
        startDate, 
        endDate
    );
    
    // Return in format optimized for Power BI
    return Ok(new
    {
        values = data.Select(d => new
        {
            date = d.Date.ToString("yyyy-MM-dd"),
            activeUsers = d.ActiveUsers,
            posts = d.PostCount,
            comments = d.CommentCount,
            engagementRate = d.EngagementRate
        })
    });
}
```

**Tableau Web Data Connector**:
```javascript
// Tableau WDC (client-side JavaScript)
(function() {
    var myConnector = tableau.makeConnector();
    
    myConnector.getSchema = function(schemaCallback) {
        var cols = [{
            id: "date",
            dataType: tableau.dataTypeEnum.date
        }, {
            id: "activeUsers",
            alias: "Active Users",
            dataType: tableau.dataTypeEnum.int
        }, {
            id: "posts",
            alias: "Posts Created",
            dataType: tableau.dataTypeEnum.int
        }];
        
        var tableSchema = {
            id: "engagementData",
            alias: "Community Engagement Data",
            columns: cols
        };
        
        schemaCallback([tableSchema]);
    };
    
    myConnector.getData = function(table, doneCallback) {
        $.ajax({
            url: "https://api.communities.com/analytics/tableau/engagement",
            headers: {
                "Authorization": "Bearer " + tableau.password
            },
            success: function(data) {
                table.appendRows(data.values);
                doneCallback();
            }
        });
    };
    
    tableau.registerConnector(myConnector);
})();
```

### Marketing Automation Integration

**Supported Platforms**:
- Marketo
- Pardot
- HubSpot Marketing
- Mailchimp

**Use Cases**:
- Trigger email campaigns based on engagement events
- Sync member segments for targeted campaigns
- Track campaign effectiveness via community activity
- Lead nurturing based on survey responses

**HubSpot Workflows**:
```csharp
public class HubSpotIntegrationService
{
    public async Task EnrollInWorkflow(User user, string workflowId)
    {
        var contact = await GetOrCreateHubSpotContact(user);
        
        var enrollment = new
        {
            email = user.Email,
            propertyValues = new[]
            {
                new { property = "engagement_score", value = await GetEngagementScore(user.Id) },
                new { property = "last_active_date", value = user.LastLoginAt }
            }
        };
        
        await _httpClient.PostAsJsonAsync(
            $"https://api.hubapi.com/automation/v2/workflows/{workflowId}/enrollments/contacts/{contact.Id}",
            enrollment
        );
    }
    
    public async Task SendEngagementAlert(Guid userId, string alertType)
    {
        var user = await _userRepository.GetById(userId);
        
        // Trigger HubSpot workflow for at-risk members
        if (alertType == "churning")
        {
            await EnrollInWorkflow(user, "re-engagement-workflow-id");
        }
    }
}
```

### AI & Transcription Services

**Azure Cognitive Services Integration**:

**Auto-Transcription** (Speech-to-Text):
```csharp
public class TranscriptionService
{
    public async Task<string> TranscribeVideoAudio(string videoUrl)
    {
        var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        speechConfig.SpeechRecognitionLanguage = "en-US";
        
        var audioConfig = AudioConfig.FromWavFileInput(await ExtractAudioFromVideo(videoUrl));
        var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        
        var transcriptBuilder = new StringBuilder();
        
        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                transcriptBuilder.AppendLine($"[{e.Result.OffsetInTicks / 10000000}s] {e.Result.Text}");
            }
        };
        
        await recognizer.StartContinuousRecognitionAsync();
        await Task.Delay(TimeSpan.FromMinutes(10)); // Or until audio complete
        await recognizer.StopContinuousRecognitionAsync();
        
        return transcriptBuilder.ToString();
    }
}
```

**Translation** (for global research):
```csharp
public class TranslationService
{
    public async Task<string> TranslateText(string text, string targetLanguage)
    {
        var translatorClient = new TextTranslationClient(
            new AzureKeyCredential(_key),
            new Uri(_endpoint)
        );
        
        var response = await translatorClient.TranslateAsync(
            targetLanguage: targetLanguage,
            content: new[] { text }
        );
        
        return response.Value[0].Translations[0].Text;
    }
    
    public async Task TranslateQualitativeResponses(Guid studyId, string targetLanguage)
    {
        var responses = await _responseRepository.GetQualitativeResponses(studyId);
        
        foreach (var response in responses.Where(r => r.Language != targetLanguage))
        {
            var translatedText = await TranslateText(response.ResponseText, targetLanguage);
            
            await _responseRepository.AddTranslation(
                response.Id,
                targetLanguage,
                translatedText
            );
        }
    }
}
```

**Sentiment Analysis**:
```csharp
public class SentimentAnalysisService
{
    public async Task AnalyzeStudySentiment(Guid studyId)
    {
        var textAnalyticsClient = new TextAnalyticsClient(
            new Uri(_endpoint),
            new AzureKeyCredential(_key)
        );
        
        var responses = await _responseRepository.GetTextResponses(studyId);
        
        // Batch process (max 10 documents per request)
        foreach (var batch in responses.Chunk(10))
        {
            var documents = batch.Select(r => r.ResponseText).ToList();
            var sentimentResults = await textAnalyticsClient.AnalyzeSentimentBatchAsync(documents);
            
            foreach (var (result, response) in sentimentResults.Value.Zip(batch))
            {
                await _responseRepository.UpdateSentiment(
                    response.Id,
                    result.DocumentSentiment.Sentiment.ToString(),
                    result.DocumentSentiment.ConfidenceScores.Positive,
                    result.DocumentSentiment.ConfidenceScores.Neutral,
                    result.DocumentSentiment.ConfidenceScores.Negative
                );
            }
        }
    }
}
```

**Third-Party Transcription** (Rev, Otter.ai):
```csharp
public class RevTranscriptionService
{
    public async Task<string> OrderTranscription(string audioUrl)
    {
        var revClient = new RevApiClient(_apiKey, _apiSecret);
        
        // Submit transcription job
        var order = await revClient.SubmitTranscriptionOrder(new
        {
            client_ref = Guid.NewGuid().ToString(),
            media_url = audioUrl,
            verbatim = true, // Include filler words like "um", "uh"
            timestamps = true,
            turnaround_time = "standard" // or "rush" for faster delivery
        });
        
        // Store order ID for webhook callback
        await _transcriptionOrderRepository.Create(new TranscriptionOrder
        {
            OrderId = order.OrderNum,
            Status = "in_progress",
            MediaUrl = audioUrl
        });
        
        return order.OrderNum;
    }
    
    [HttpPost("webhooks/rev/transcription")]
    public async Task<IActionResult> ReceiveRevWebhook([FromBody] RevWebhookPayload payload)
    {
        if (payload.Status == "complete")
        {
            var transcript = await _revClient.GetTranscript(payload.OrderNum);
            
            await _transcriptionOrderRepository.UpdateTranscript(
                payload.OrderNum,
                transcript.Text,
                transcript.Segments // Timestamped segments
            );
        }
        
        return Ok();
    }
}
```

### Video & Media Processing

**Secure Video Ingestion** (SFTP/REST):

**SFTP Server for Large Uploads**:
```csharp
public class SftpIngestionService
{
    public async Task SetupTenantSftpAccount(Guid tenantId)
    {
        var credentials = GenerateSftpCredentials();
        
        // Create isolated directory for tenant
        var sftpPath = $"/uploads/{tenantId}/";
        await CreateSftpDirectory(sftpPath);
        
        // Set up watcher for new files
        _fileSystemWatcher.Path = sftpPath;
        _fileSystemWatcher.Created += async (s, e) =>
        {
            await ProcessUploadedVideo(tenantId, e.FullPath);
        };
        _fileSystemWatcher.EnableRaisingEvents = true;
        
        return new SftpAccountInfo
        {
            Host = _sftpHost,
            Port = 22,
            Username = credentials.Username,
            Password = credentials.Password,
            Path = sftpPath
        };
    }
    
    private async Task ProcessUploadedVideo(Guid tenantId, string filePath)
    {
        // Move to Azure Blob Storage
        var blobUrl = await UploadToBlobStorage(tenantId, filePath);
        
        // Trigger transcoding job
        await _mediaServicesClient.StartTranscodingJob(blobUrl);
        
        // Clean up SFTP file
        File.Delete(filePath);
    }
}
```

**Chunked Upload API** (for large video files):
```csharp
[HttpPost("api/v1/media/upload/init")]
public async Task<IActionResult> InitiateChunkedUpload([FromBody] UploadInitRequest request)
{
    var uploadSession = new UploadSession
    {
        Id = Guid.NewGuid(),
        TenantId = User.TenantId(),
        FileName = request.FileName,
        FileSize = request.FileSize,
        TotalChunks = (int)Math.Ceiling(request.FileSize / (double)_chunkSize),
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };
    
    await _uploadSessionRepository.Create(uploadSession);
    
    return Ok(new { UploadSessionId = uploadSession.Id, ChunkSize = _chunkSize });
}

[HttpPost("api/v1/media/upload/chunk")]
public async Task<IActionResult> UploadChunk(
    [FromQuery] Guid uploadSessionId,
    [FromQuery] int chunkIndex,
    [FromForm] IFormFile chunk)
{
    var session = await _uploadSessionRepository.GetById(uploadSessionId);
    
    // Save chunk to temporary storage
    var chunkPath = Path.Combine(_tempPath, $"{uploadSessionId}_{chunkIndex}");
    using (var stream = new FileStream(chunkPath, FileMode.Create))
    {
        await chunk.CopyToAsync(stream);
    }
    
    // Update progress
    await _uploadSessionRepository.MarkChunkReceived(uploadSessionId, chunkIndex);
    
    // Check if all chunks received
    if (await AllChunksReceived(uploadSessionId))
    {
        await FinalizeUpload(uploadSessionId);
    }
    
    return Ok(new { ChunksReceived = session.ReceivedChunks.Count, TotalChunks = session.TotalChunks });
}

private async Task FinalizeUpload(Guid uploadSessionId)
{
    var session = await _uploadSessionRepository.GetById(uploadSessionId);
    
    // Concatenate chunks
    var finalFilePath = Path.Combine(_tempPath, session.FileName);
    using (var outputStream = new FileStream(finalFilePath, FileMode.Create))
    {
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = Path.Combine(_tempPath, $"{uploadSessionId}_{i}");
            using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
            {
                await chunkStream.CopyToAsync(outputStream);
            }
            File.Delete(chunkPath); // Clean up chunk
        }
    }
    
    // Upload to Azure Blob Storage
    var blobUrl = await UploadToBlobStorage(session.TenantId, finalFilePath);
    
    // Clean up temp file
    File.Delete(finalFilePath);
    
    // Mark session complete
    await _uploadSessionRepository.MarkComplete(uploadSessionId, blobUrl);
}
```

**Azure Media Services Integration**:
```csharp
public class MediaProcessingService
{
    public async Task<string> TranscodeVideo(string inputBlobUrl)
    {
        var mediaServicesClient = CreateMediaServicesClient();
        
        // Create input asset
        var inputAsset = await mediaServicesClient.Assets.CreateOrUpdateAsync(
            _resourceGroup,
            _accountName,
            $"input-{Guid.NewGuid()}",
            new Asset()
        );
        
        // Copy blob to input asset
        await CopyBlobToAsset(inputBlobUrl, inputAsset);
        
        // Create output asset
        var outputAsset = await mediaServicesClient.Assets.CreateOrUpdateAsync(
            _resourceGroup,
            _accountName,
            $"output-{Guid.NewGuid()}",
            new Asset()
        );
        
        // Create transform (encoding preset)
        var transform = await GetOrCreateTransform(mediaServicesClient);
        
        // Submit job
        var job = await mediaServicesClient.Jobs.CreateAsync(
            _resourceGroup,
            _accountName,
            transform.Name,
            $"job-{Guid.NewGuid()}",
            new Job
            {
                Input = new JobInputAsset(inputAsset.Name),
                Outputs = new[]
                {
                    new JobOutputAsset(outputAsset.Name)
                }
            }
        );
        
        // Wait for completion (or use webhook)
        job = await WaitForJobToFinish(mediaServicesClient, transform.Name, job.Name);
        
        // Get output URL
        var streamingLocator = await CreateStreamingLocator(mediaServicesClient, outputAsset.Name);
        
        return streamingLocator.StreamingUrl;
    }
    
    private async Task<Transform> GetOrCreateTransform(IAzureMediaServicesClient client)
    {
        var transformName = "AdaptiveBitrate720p";
        
        try
        {
            return await client.Transforms.GetAsync(_resourceGroup, _accountName, transformName);
        }
        catch
        {
            // Create transform with preset
            var outputs = new[]
            {
                new TransformOutput(new StandardEncoderPreset(
                    codecs: new Codec[]
                    {
                        new AacAudio { Channels = 2, SamplingRate = 48000, Bitrate = 128000 },
                        new H264Video
                        {
                            Layers = new[]
                            {
                                new H264Layer { Bitrate = 2000000, Width = "1280", Height = "720" }
                            }
                        }
                    },
                    formats: new Format[]
                    {
                        new Mp4Format { FilenamePattern = "{Basename}_{Bitrate}{Extension}" }
                    }
                ))
            };
            
            return await client.Transforms.CreateOrUpdateAsync(
                _resourceGroup,
                _accountName,
                transformName,
                outputs
            );
        }
    }
}
```

## Public API Strategy

### REST API

**API Design Principles**:
- RESTful resource-oriented URLs
- JSON request/response bodies
- OAuth 2.0 authentication
- Rate limiting per API key
- Versioning via URL path (`/api/v1/`)
- Consistent error responses (RFC 7807 Problem Details)

**Public Endpoints**:

```csharp
// Communities
GET    /api/v1/communities                      // List communities
GET    /api/v1/communities/{id}                 // Get community details
POST   /api/v1/communities                      // Create community (admin only)

// Posts
GET    /api/v1/communities/{id}/posts           // List posts in community
GET    /api/v1/posts/{id}                       // Get post details
POST   /api/v1/posts                            // Create post
PUT    /api/v1/posts/{id}                       // Update post
DELETE /api/v1/posts/{id}                       // Delete post

// Members
GET    /api/v1/communities/{id}/members         // List members
POST   /api/v1/communities/{id}/members         // Add member

// Surveys
GET    /api/v1/surveys                          // List surveys
GET    /api/v1/surveys/{id}                     // Get survey details
POST   /api/v1/surveys/{id}/responses           // Submit response

// Analytics
GET    /api/v1/analytics/engagement             // Engagement metrics
GET    /api/v1/analytics/surveys/{id}/results   // Survey results
```

**API Authentication**:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class PostsController : ControllerBase
{
    [HttpGet]
    [RateLimit(RequestsPerMinute = 100)]
    public async Task<ActionResult<PagedResult<Post>>> GetPosts(
        [FromQuery] Guid communityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var posts = await _postService.GetPosts(
            User.TenantId(),
            communityId,
            page,
            pageSize
        );
        
        return Ok(posts);
    }
}
```

**API Client SDK** (auto-generated from OpenAPI):
```csharp
// C# SDK
var client = new CommunitiesApiClient("https://api.communities.com", apiKey);
var posts = await client.Posts.ListAsync(communityId, page: 1, pageSize: 20);

// JavaScript SDK
const client = new CommunitiesClient('https://api.communities.com', { apiKey });
const posts = await client.posts.list({ communityId, page: 1, pageSize: 20 });

// Python SDK
client = CommunitiesClient('https://api.communities.com', api_key=api_key)
posts = client.posts.list(community_id=community_id, page=1, page_size=20)
```

### GraphQL API (Future)

**Schema**:
```graphql
type Query {
  community(id: ID!): Community
  communities(filter: CommunityFilter, limit: Int = 20, offset: Int = 0): [Community!]!
  post(id: ID!): Post
  posts(communityId: ID!, limit: Int = 20, offset: Int = 0): [Post!]!
  me: User
}

type Mutation {
  createPost(input: CreatePostInput!): Post!
  updatePost(id: ID!, input: UpdatePostInput!): Post!
  deletePost(id: ID!): Boolean!
  createComment(postId: ID!, content: String!): Comment!
  addReaction(postId: ID!, reactionType: ReactionType!): Reaction!
}

type Subscription {
  postCreated(communityId: ID!): Post!
  commentAdded(postId: ID!): Comment!
}

type Community {
  id: ID!
  name: String!
  description: String
  memberCount: Int!
  posts(limit: Int = 20, offset: Int = 0): [Post!]!
  members(limit: Int = 20, offset: Int = 0): [User!]!
}

type Post {
  id: ID!
  title: String
  content: String!
  author: User!
  community: Community!
  comments: [Comment!]!
  reactions: [Reaction!]!
  createdAt: DateTime!
}

type User {
  id: ID!
  displayName: String!
  avatar: String
  posts: [Post!]!
  engagementScore: Float
}
```

**Implementation** (Hot Chocolate):
```csharp
public class Query
{
    public async Task<Community> GetCommunity(
        [Service] ICommunityRepository repository,
        Guid id)
    {
        return await repository.GetById(id);
    }
    
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Post> GetPosts(
        [Service] AppDbContext context,
        Guid communityId)
    {
        return context.Posts.Where(p => p.CommunityId == communityId);
    }
}

public class Mutation
{
    public async Task<Post> CreatePost(
        [Service] IPostService postService,
        CreatePostInput input,
        ClaimsPrincipal principal)
    {
        var tenantId = principal.TenantId();
        var userId = principal.UserId();
        
        return await postService.CreatePost(
            tenantId,
            userId,
            input.CommunityId,
            input.Title,
            input.Content
        );
    }
}

public class Subscription
{
    [Subscribe]
    public Post PostCreated(
        [EventMessage] Post post,
        Guid communityId)
    {
        return post.CommunityId == communityId ? post : null;
    }
}
```

## Webhook System

### Webhook Configuration

**Per-Tenant Webhook Registration**:
```csharp
public class Webhook
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Url { get; set; }
    public List<string> Events { get; set; } // ["post.created", "survey.completed"]
    public string Secret { get; set; } // For HMAC signature
    public bool IsActive { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
}

[HttpPost("api/v1/webhooks")]
[Authorize(Policy = "TenantAdmin")]
public async Task<ActionResult<Webhook>> CreateWebhook(CreateWebhookDto dto)
{
    var webhook = new Webhook
    {
        TenantId = User.TenantId(),
        Url = dto.Url,
        Events = dto.Events,
        Secret = GenerateSecret(),
        IsActive = true
    };
    
    await _webhookRepository.Create(webhook);
    
    return CreatedAtAction(nameof(GetWebhook), new { id = webhook.Id }, webhook);
}
```

### Webhook Delivery

```csharp
public class WebhookDeliveryService
{
    public async Task SendWebhook(Guid tenantId, string eventType, object payload)
    {
        var webhooks = await _webhookRepository.GetByTenantAndEvent(tenantId, eventType);
        
        foreach (var webhook in webhooks.Where(w => w.IsActive))
        {
            await DeliverWebhook(webhook, eventType, payload);
        }
    }
    
    private async Task DeliverWebhook(Webhook webhook, string eventType, object payload)
    {
        var webhookPayload = new
        {
            eventId = Guid.NewGuid(),
            eventType = eventType,
            timestamp = DateTime.UtcNow,
            data = payload
        };
        
        var json = JsonSerializer.Serialize(webhookPayload);
        var signature = ComputeHmacSignature(json, webhook.Secret);
        
        var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Webhook-Signature", signature);
        request.Headers.Add("X-Webhook-ID", webhook.Id.ToString());
        
        try
        {
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                webhook.LastSuccessAt = DateTime.UtcNow;
                webhook.FailureCount = 0;
            }
            else
            {
                await HandleWebhookFailure(webhook, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            await HandleWebhookFailure(webhook, null, ex);
        }
        
        await _webhookRepository.Update(webhook);
    }
    
    private async Task HandleWebhookFailure(Webhook webhook, HttpStatusCode? statusCode, Exception ex = null)
    {
        webhook.FailureCount++;
        webhook.LastFailureAt = DateTime.UtcNow;
        
        // Disable webhook after 10 consecutive failures
        if (webhook.FailureCount >= 10)
        {
            webhook.IsActive = false;
            await _notificationService.NotifyAdmin(
                webhook.TenantId,
                $"Webhook {webhook.Id} disabled due to repeated failures"
            );
        }
        
        _logger.LogError(ex, "Webhook delivery failed. URL: {Url}, Status: {Status}", 
            webhook.Url, statusCode);
    }
    
    private string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
```

### Webhook Retry Strategy

**Exponential Backoff**:
- Retry 1: Immediate
- Retry 2: 5 seconds
- Retry 3: 25 seconds
- Retry 4: 125 seconds (~2 minutes)
- Retry 5: 625 seconds (~10 minutes)

**Implementation** (using Hangfire):
```csharp
public class WebhookRetryService
{
    [AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [RetryFilter(typeof(ExponentialBackoffRetryFilter))]
    public async Task DeliverWithRetry(Guid webhookId, string eventType, object payload)
    {
        var webhook = await _webhookRepository.GetById(webhookId);
        await _deliveryService.DeliverWebhook(webhook, eventType, payload);
    }
}

public class ExponentialBackoffRetryFilter : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        var failedState = context.CandidateState as FailedState;
        if (failedState != null)
        {
            var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;
            var delay = TimeSpan.FromSeconds(Math.Pow(5, retryAttempt));
            
            context.CandidateState = new ScheduledState(delay)
            {
                Reason = $"Retry attempt {retryAttempt} after {delay.TotalSeconds} seconds"
            };
            
            context.SetJobParameter("RetryCount", retryAttempt);
        }
    }
}
```

### Webhook Verification (Client-Side)

```csharp
// Client receives webhook and verifies signature
public class WebhookController : ControllerBase
{
    [HttpPost("webhooks/communities")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        var signature = Request.Headers["X-Webhook-Signature"].ToString();
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        
        if (!VerifySignature(payload, signature, _webhookSecret))
        {
            return Unauthorized("Invalid signature");
        }
        
        var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(payload);
        
        // Process event
        await _eventProcessor.Process(webhookEvent);
        
        return Ok();
    }
    
    private bool VerifySignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToBase64String(hash);
        
        return signature == computedSignature;
    }
}
```

## Plugin System (Future)

### Plugin Architecture

**Plugin Manifest**:
```json
{
  "id": "gamification-plugin",
  "name": "Gamification",
  "version": "1.0.0",
  "description": "Add points, badges, and leaderboards",
  "author": "Platform Team",
  "entryPoint": "GamificationPlugin.dll",
  "permissions": [
    "read:posts",
    "read:comments",
    "write:custom-entities"
  ],
  "hooks": [
    "post.created",
    "comment.added"
  ],
  "uiComponents": [
    {
      "name": "LeaderboardWidget",
      "location": "community.sidebar",
      "component": "LeaderboardComponent"
    }
  ]
}
```

**Plugin Interface**:
```csharp
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    
    Task Initialize(IPluginContext context);
    Task<PluginResponse> HandleEvent(PluginEvent evt);
    Task Shutdown();
}

public class GamificationPlugin : IPlugin
{
    public string Id => "gamification-plugin";
    public string Name => "Gamification";
    public Version Version => new Version(1, 0, 0);
    
    public async Task Initialize(IPluginContext context)
    {
        // Register event handlers
        context.On("post.created", OnPostCreated);
        context.On("comment.added", OnCommentAdded);
        
        // Create custom database tables
        await context.Database.ExecuteSql(@"
            CREATE TABLE IF NOT EXISTS UserPoints (
                UserId uniqueidentifier,
                Points int,
                PRIMARY KEY (UserId)
            )
        ");
    }
    
    private async Task OnPostCreated(PluginEvent evt)
    {
        var userId = evt.Data["authorId"];
        await AwardPoints(userId, 10); // 10 points for creating a post
    }
    
    private async Task OnCommentAdded(PluginEvent evt)
    {
        var userId = evt.Data["authorId"];
        await AwardPoints(userId, 5); // 5 points for commenting
    }
}
```

## Data Synchronization Patterns

### Real-Time Sync (Webhooks)
- Immediate notification of changes
- Low latency
- Requires recipient to be always available

### Batch Sync (Scheduled Jobs)
- Periodic synchronization (hourly, daily)
- More efficient for large volumes
- Acceptable delay for non-critical data

### Hybrid Sync
- Real-time for critical events
- Batch for historical data and bulk updates

## API Versioning

### URL-Based Versioning
```
/api/v1/posts
/api/v2/posts
```

**Version Deprecation Policy**:
- Minimum 12 months support for deprecated versions
- 6-month advance notice of deprecation
- Migration guide provided
- Deprecation warnings in API responses

**Response Headers**:
```
X-API-Version: 1.0
X-API-Deprecated: false
Sunset: Sat, 31 Dec 2025 23:59:59 GMT
```

## API Documentation

### OpenAPI/Swagger

**Automatic Generation**:
```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Online Communities API",
        Version = "v1",
        Description = "RESTful API for community engagement platform",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "api-support@communities.com"
        }
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    
    // Add JWT authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```

### Developer Portal

**Content**:
- Getting started guide
- Authentication tutorial
- API reference (interactive)
- Code samples (C#, JavaScript, Python)
- Webhook setup guide
- Rate limits and quotas
- Changelog
- Status page

**Interactive API Explorer**:
- Try API calls directly in browser
- OAuth 2.0 authentication flow
- Request/response examples
- Error code reference

## Rate Limiting and Quotas

**Tiered Rate Limits**:
- Free tier: 100 requests/hour
- Standard tier: 1,000 requests/hour
- Premium tier: 10,000 requests/hour
- Enterprise tier: Custom limits

**Implementation**:
```csharp
public class ApiRateLimitMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].ToString();
        var tier = await _apiKeyService.GetTier(apiKey);
        var limit = GetRateLimit(tier);
        
        var key = $"ratelimit:api:{apiKey}:{DateTime.UtcNow.Hour}";
        var current = await _cache.StringIncrementAsync(key);
        
        if (current == 1)
        {
            await _cache.KeyExpireAsync(key, TimeSpan.FromHours(1));
        }
        
        context.Response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", Math.Max(0, limit - current).ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
        
        if (current > limit)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        await _next(context);
    }
}
```

## Backward Compatibility

**Breaking Changes** (require new version):
- Removing or renaming fields
- Changing field types
- Removing endpoints
- Changing authentication requirements

**Non-Breaking Changes** (same version):
- Adding new fields (optional)
- Adding new endpoints
- Adding new optional parameters
- Deprecating (but not removing) fields

**Change Log**:
```markdown
## Version 2.0.0 (2025-06-01)
### Breaking Changes
- Renamed `Post.authorId` to `Post.author.id`
- Removed deprecated `GET /posts/all` endpoint (use `/posts` with filter)

### New Features
- Added GraphQL API endpoint
- Added webhook subscriptions

## Version 1.1.0 (2025-03-01)
### New Features
- Added `engagementScore` field to User
- Added `GET /analytics/engagement` endpoint

### Deprecated
- `GET /posts/all` (use `/posts` instead)
```

