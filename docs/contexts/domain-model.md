# Business Domain Model

## Core Entities

### Tenant

**Description**: Top-level organization representing a client using the platform.

**Attributes**:
- `Id` (Guid): Unique identifier
- `Name` (string): Organization name
- `Domain` (string): Custom subdomain or domain (e.g., `acme.communities.com`)
- `Status` (enum): Active, Suspended, Inactive
- `SubscriptionTier` (enum): Free, Standard, Premium, Enterprise
- `Settings` (JSON): Tenant-specific configuration
- `BrandingConfig` (JSON): Logo, colors, fonts
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

**Relationships**:
- Has many `Communities`
- Has many `Users` (through memberships)
- Has one `BillingAccount`
- Has many `FeatureFlags`

**Business Rules**:
- Domain must be unique across all tenants
- Cannot delete tenant with active communities
- Suspended tenants cannot create new content

**Aggregate Root**: Yes

---

### User

**Description**: Individual person who interacts with the platform. Users belong to one or more tenants.

**Attributes**:
- `Id` (Guid): Unique identifier
- `Email` (string): Unique email address
- `PasswordHash` (string): Hashed password
- `FirstName` (string)
- `LastName` (string)
- `DisplayName` (string): Public-facing name
- `Avatar` (string): URL to profile picture
- `Bio` (string): User biography
- `Status` (enum): Active, Suspended, Deleted
- `EmailVerified` (bool)
- `CreatedAt` (DateTime)
- `LastLoginAt` (DateTime)

**Relationships**:
- Has many `Memberships` (links to communities)
- Has many `Posts`
- Has many `Comments`
- Has many `Reactions`
- Has many `SurveyResponses`
- Belongs to many `Roles` (through UserRoles)

**Business Rules**:
- Email must be unique globally
- Display name must be unique within tenant
- Password must meet complexity requirements
- Cannot delete user with active content (must anonymize instead)

**Aggregate Root**: Yes

---

### Community

**Description**: Branded space for engagement within a tenant. A tenant can have multiple communities.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `Name` (string): Community name
- `Description` (string): Purpose and guidelines
- `Slug` (string): URL-friendly identifier
- `Visibility` (enum): Public, Private, Hidden
- `Status` (enum): Active, Archived
- `Settings` (JSON): Community-specific configuration
- `MemberCount` (int): Cached member count
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Has many `Groups`
- Has many `Memberships`
- Has many `Posts`
- Has many `Surveys`
- Has many `Polls`

**Business Rules**:
- Slug must be unique within tenant
- Private communities require invitation or approval to join
- Archived communities are read-only

**Aggregate Root**: Yes

---

### Group

**Description**: Sub-community or segment within a community for organizing members or content.

**Attributes**:
- `Id` (Guid): Unique identifier
- `CommunityId` (Guid): Foreign key to Community
- `Name` (string): Group name
- `Description` (string)
- `Visibility` (enum): Public, Private
- `MemberCount` (int): Cached member count
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `Community`
- Has many `Memberships`
- Has many `Posts` (optionally scoped to group)

**Business Rules**:
- Group names must be unique within community
- Private groups require explicit membership

**Aggregate Root**: No (part of Community aggregate)

---

### Membership

**Description**: Junction entity representing a user's participation in a community or group.

**Attributes**:
- `Id` (Guid): Unique identifier
- `UserId` (Guid): Foreign key to User
- `CommunityId` (Guid): Foreign key to Community (nullable)
- `GroupId` (Guid): Foreign key to Group (nullable)
- `Role` (enum): Member, Moderator, Admin
- `Status` (enum): Active, Suspended, Left
- `JoinedAt` (DateTime)
- `LastActiveAt` (DateTime)

**Relationships**:
- Belongs to one `User`
- Belongs to one `Community` (if community membership)
- Belongs to one `Group` (if group membership)

**Business Rules**:
- User can only have one active membership per community
- Moderators can manage content but not change community settings
- Admins have full control over community

**Aggregate Root**: No (part of Community aggregate)

---

### Post

**Description**: User-generated content shared within a community.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant (for query optimization)
- `CommunityId` (Guid): Foreign key to Community
- `GroupId` (Guid): Foreign key to Group (nullable)
- `AuthorId` (Guid): Foreign key to User
- `Title` (string): Optional title
- `Content` (string): Post body (Markdown or rich text)
- `ContentType` (enum): Text, Image, Video, Link
- `MediaUrls` (JSON array): Attached media files
- `Status` (enum): Published, Draft, Flagged, Removed
- `IsPinned` (bool): Featured post
- `ViewCount` (int): Cached view count
- `ReactionCount` (int): Cached reaction count
- `CommentCount` (int): Cached comment count
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)
- `PublishedAt` (DateTime)

**Relationships**:
- Belongs to one `User` (author)
- Belongs to one `Community`
- Belongs to one `Group` (optional)
- Has many `Comments`
- Has many `Reactions`
- Has many `PostViews`
- Has many `Mentions`

**Business Rules**:
- Published posts cannot be deleted (only soft-deleted)
- Flagged posts require moderator review
- Content must not exceed character limit (configurable per tenant)
- Pinned posts appear at top of feed

**Aggregate Root**: Yes

**Domain Events**:
- `PostCreated`
- `PostPublished`
- `PostEdited`
- `PostFlagged`
- `PostRemoved`

---

### Comment

**Description**: Response to a post or another comment (threaded).

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `PostId` (Guid): Foreign key to Post
- `ParentCommentId` (Guid): Foreign key to Comment (nullable, for threading)
- `AuthorId` (Guid): Foreign key to User
- `Content` (string): Comment text
- `Status` (enum): Published, Flagged, Removed
- `ReactionCount` (int): Cached reaction count
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

**Relationships**:
- Belongs to one `Post`
- Belongs to one `User` (author)
- Belongs to one `Comment` (parent, optional)
- Has many `Comments` (children)
- Has many `Reactions`

**Business Rules**:
- Comment nesting limited to 3 levels
- Cannot comment on removed posts
- Flagged comments require moderator review

**Aggregate Root**: No (part of Post aggregate)

**Domain Events**:
- `CommentAdded`
- `CommentEdited`
- `CommentFlagged`
- `CommentRemoved`

---

### Reaction

**Description**: Simple engagement indicator (like, upvote, sentiment).

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `UserId` (Guid): Foreign key to User
- `TargetType` (enum): Post, Comment
- `TargetId` (Guid): Foreign key to Post or Comment
- `ReactionType` (enum): Like, Love, Insightful, Upvote, Downvote
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `User`
- Belongs to one `Post` or one `Comment` (polymorphic)

**Business Rules**:
- User can only have one reaction per target
- Changing reaction replaces previous one
- Cannot react to removed content

**Aggregate Root**: No (part of Post aggregate)

**Domain Events**:
- `ReactionAdded`
- `ReactionChanged`
- `ReactionRemoved`

---

### Poll

**Description**: Quick voting mechanism for gathering opinions.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `CreatorId` (Guid): Foreign key to User
- `Question` (string): Poll question
- `Options` (JSON array): List of choices
- `AllowMultipleChoices` (bool)
- `IsAnonymous` (bool): Hide voter identities
- `Status` (enum): Open, Closed
- `ExpiresAt` (DateTime): Optional closing date
- `VoteCount` (int): Cached total votes
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `Community`
- Belongs to one `User` (creator)
- Has many `PollVotes`

**Business Rules**:
- Must have at least 2 options
- Cannot edit options after votes are cast
- Closed polls are read-only

**Aggregate Root**: Yes

**Domain Events**:
- `PollCreated`
- `PollVoteSubmitted`
- `PollClosed`

---

### PollVote

**Description**: Individual vote on a poll option.

**Attributes**:
- `Id` (Guid): Unique identifier
- `PollId` (Guid): Foreign key to Poll
- `UserId` (Guid): Foreign key to User
- `OptionId` (string): Selected option(s)
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `Poll`
- Belongs to one `User`

**Business Rules**:
- User can only vote once (unless multiple choices allowed)
- Cannot change vote after submission (unless poll allows)

**Aggregate Root**: No (part of Poll aggregate)

---

### Survey

**Description**: Multi-question research instrument for data collection.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `CreatorId` (Guid): Foreign key to User
- `Title` (string): Survey name
- `Description` (string): Purpose and instructions
- `Status` (enum): Draft, Published, Closed, Archived
- `IsAnonymous` (bool): Hide respondent identities
- `AllowMultipleResponses` (bool)
- `PublishedAt` (DateTime)
- `ExpiresAt` (DateTime): Optional closing date
- `ResponseCount` (int): Cached count
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

**Relationships**:
- Belongs to one `Community`
- Belongs to one `User` (creator)
- Has many `Questions`
- Has many `SurveyResponses`

**Business Rules**:
- Must have at least one question
- Cannot edit questions after responses are collected (must create new version)
- Published surveys visible to community members

**Aggregate Root**: Yes

**Domain Events**:
- `SurveyCreated`
- `SurveyPublished`
- `SurveyResponseSubmitted`
- `SurveyClosed`

---

### Question

**Description**: Individual question within a survey.

**Attributes**:
- `Id` (Guid): Unique identifier
- `SurveyId` (Guid): Foreign key to Survey
- `QuestionText` (string): Question prompt
- `QuestionType` (enum): Text, MultipleChoice, Checkbox, Scale, Matrix, Date
- `Options` (JSON): Answer choices (for choice questions)
- `Validation` (JSON): Min/max length, required, etc.
- `Logic` (JSON): Branching and skip logic
- `Order` (int): Display sequence
- `IsRequired` (bool)

**Relationships**:
- Belongs to one `Survey`
- Has many `Responses` (answers)

**Business Rules**:
- Order must be unique within survey
- Choice questions must have at least 2 options
- Logic can reference other questions in survey

**Aggregate Root**: No (part of Survey aggregate)

---

### SurveyResponse

**Description**: User's submission of a completed survey.

**Attributes**:
- `Id` (Guid): Unique identifier
- `SurveyId` (Guid): Foreign key to Survey
- `RespondentId` (Guid): Foreign key to User (nullable for anonymous)
- `Status` (enum): InProgress, Completed, Abandoned
- `Answers` (JSON): Question ID to answer mapping
- `StartedAt` (DateTime)
- `CompletedAt` (DateTime)
- `IpAddress` (string): For fraud detection
- `UserAgent` (string)

**Relationships**:
- Belongs to one `Survey`
- Belongs to one `User` (respondent, optional)
- Has many `QuestionResponses`

**Business Rules**:
- Required questions must be answered before completion
- Cannot modify after completion (unless survey allows)
- Abandoned responses deleted after 30 days

**Aggregate Root**: No (part of Survey aggregate)

**Domain Events**:
- `SurveyResponseStarted`
- `SurveyResponseCompleted`

---

### Notification

**Description**: Message delivered to a user via various channels.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `UserId` (Guid): Foreign key to User
- `Type` (enum): System, Mention, Comment, Reaction, Survey, Moderation
- `Title` (string): Notification headline
- `Message` (string): Notification body
- `ActionUrl` (string): Deep link to related content
- `Channel` (enum): InApp, Email, SMS, Push
- `Status` (enum): Pending, Sent, Read, Failed
- `Priority` (enum): Low, Normal, High, Urgent
- `SentAt` (DateTime)
- `ReadAt` (DateTime)
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `User`
- References related entity (Post, Comment, Survey, etc.)

**Business Rules**:
- User preferences control which notifications are sent
- Digest mode batches multiple notifications
- Failed notifications retry up to 3 times

**Aggregate Root**: No (managed by Notification Service)

**Domain Events**:
- `NotificationCreated`
- `NotificationSent`
- `NotificationRead`

---

### AuditLog

**Description**: Immutable record of significant actions for compliance and security.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `UserId` (Guid): Foreign key to User (nullable)
- `Action` (string): Action taken (e.g., "post.create", "user.suspend")
- `EntityType` (string): Affected entity type
- `EntityId` (Guid): Affected entity ID
- `OldValue` (JSON): State before change
- `NewValue` (JSON): State after change
- `IpAddress` (string)
- `UserAgent` (string)
- `Timestamp` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `User` (actor, optional)

**Business Rules**:
- Cannot be modified or deleted
- Retained per compliance requirements (7 years minimum)
- Sensitive data masked in logs

**Aggregate Root**: No (managed by Admin Service)

---

## Bounded Contexts

### Identity & Access Context

**Entities**: User, Role, Permission, UserRole

**Responsibilities**:
- User registration and authentication
- Role and permission management
- Authorization decisions

**Integration Points**:
- Publishes `UserRegistered`, `UserActivated` events
- Consumes no events from other contexts

---

### Community Context

**Entities**: Tenant, Community, Group, Membership

**Responsibilities**:
- Community and group lifecycle
- Membership management
- Access control policies

**Integration Points**:
- Publishes `CommunityCreated`, `MemberJoined` events
- Consumes `UserRegistered` for auto-membership

---

### Engagement Context

**Entities**: Post, Comment, Reaction, Mention

**Responsibilities**:
- Content creation and moderation
- Social interactions (comments, reactions)
- Feed generation

**Integration Points**:
- Publishes `PostCreated`, `CommentAdded`, `ReactionAdded` events
- Consumes `MemberJoined` to show welcome posts

---

### Research Context

**Entities**: Survey, Question, Poll, SurveyResponse, PollVote

**Responsibilities**:
- Survey and poll creation
- Response collection
- Results aggregation

**Integration Points**:
- Publishes `SurveyPublished`, `SurveyResponseSubmitted` events
- Consumes no events from other contexts

---

### Analytics Context

**Entities**: EngagementMetric, MemberSegment, Report

**Responsibilities**:
- Data aggregation and metrics calculation
- Member segmentation
- Report generation

**Integration Points**:
- Consumes all events from other contexts for analytics

---

### Notification Context

**Entities**: Notification, NotificationPreference, NotificationTemplate

**Responsibilities**:
- Multi-channel notification delivery
- Preference management
- Template rendering

**Integration Points**:
- Consumes all events that trigger notifications
- Publishes `NotificationSent` events

---

## Domain Events

**Naming Convention**: `{Entity}{Action}` (past tense)

**Structure**:
```csharp
public record PostCreated(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid PostId,
    Guid CommunityId,
    Guid AuthorId,
    string Title
) : DomainEvent;
```

**Key Events**:
- `UserRegistered`, `UserActivated`, `UserDeactivated`
- `CommunityCreated`, `MemberJoined`, `MemberLeft`
- `PostCreated`, `PostPublished`, `PostFlagged`
- `CommentAdded`, `ReactionAdded`
- `SurveyPublished`, `SurveyResponseSubmitted`
- `PollCreated`, `PollVoteSubmitted`, `PollClosed`

---

## Extension Points

### Custom Fields

**Mechanism**: JSON columns on core entities for tenant-defined attributes

**Example**:
```json
{
  "User.CustomFields": {
    "employeeId": "12345",
    "department": "Marketing",
    "tier": "Gold"
  }
}
```

**Constraints**:
- Indexed for query performance
- Validated against tenant-defined schema
- Searchable in member directory

---

### Custom Entities (Future)

**Mechanism**: Tenant can define entirely new entity types

**Use Case**: Industry-specific data (e.g., product reviews for retail community)

**Implementation**:
- Dynamic schema stored in `CustomEntityDefinitions` table
- Data stored in schemaless Cosmos DB
- Queryable via GraphQL API

---

## UML-Style Representation

```
┌─────────────┐
│   Tenant    │
└──────┬──────┘
       │ 1
       │
       │ *
┌──────▼──────────┐
│   Community     │
└──────┬──────────┘
       │ 1
       │
       │ *
┌──────▼──────┐       ┌──────────┐
│  Membership │───────│   User   │
└─────────────┘  *  * └──────────┘
       │
       │
┌──────▼──────┐
│    Post     │
└──────┬──────┘
       │ 1
       │
       │ *
┌──────▼──────┐
│   Comment   │
└─────────────┘

┌─────────────┐
│   Survey    │
└──────┬──────┘
       │ 1
       │
       │ *
┌──────▼──────────┐
│   Question      │
└─────────────────┘
       │ 1
       │
       │ *
┌──────▼──────────────┐
│  SurveyResponse     │
└─────────────────────┘
```

---

## Research-Specific Entities

### ResearchTask

**Description**: Qualitative research activity (e.g., video diary, photo diary, collage, image annotation).

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `Title` (string): Task name (e.g., "Share your morning routine")
- `Instructions` (string): Detailed prompt for participants
- `TaskType` (enum): VideoDiary, PhotoDiary, ImageAnnotation, Collage
- `Status` (enum): Draft, Active, Closed
- `StartDate` (DateTime): When task becomes available
- `EndDate` (DateTime): Deadline for submissions
- `MaxSubmissions` (int): Limit per participant
- `RequiresModeration` (bool): Submissions must be reviewed before visible
- `CreatedBy` (Guid): User who created task
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `Community`
- Has many `TaskSubmissions`

**Business Rules**:
- End date must be after start date
- Cannot delete task with submissions (must archive instead)
- Active tasks can accept submissions

**Aggregate Root**: Yes

---

### TaskSubmission

**Description**: Participant's response to a qualitative research task.

**Attributes**:
- `Id` (Guid): Unique identifier
- `ResearchTaskId` (Guid): Foreign key to ResearchTask
- `ParticipantId` (Guid): Foreign key to User
- `MediaUrl` (string): URL to uploaded video/photo
- `MediaType` (string): MIME type
- `Caption` (string): Optional text commentary
- `Metadata` (JSON): Duration, dimensions, location, etc.
- `Status` (enum): Submitted, Approved, Rejected, Flagged
- `ModerationNote` (string): Feedback from moderator
- `SubmittedAt` (DateTime)
- `ReviewedAt` (DateTime)
- `ReviewedBy` (Guid): Moderator who reviewed

**Relationships**:
- Belongs to one `ResearchTask`
- Belongs to one `User` (participant)
- Has many `MediaAnnotations`

**Business Rules**:
- Submissions after deadline are marked late
- Media must be scanned for viruses before storage
- PII in submissions must be redacted before export

**Aggregate Root**: No (part of ResearchTask aggregate)

---

### Interview

**Description**: One-on-one in-depth interview session (IDI).

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `Title` (string): Interview topic
- `ParticipantId` (Guid): Foreign key to User
- `ModeratorId` (Guid): Interviewer
- `ScheduledAt` (DateTime): Appointment time
- `Duration` (int): Expected duration in minutes
- `Status` (enum): Scheduled, InProgress, Completed, Cancelled
- `MeetingUrl` (string): Video call link
- `RecordingUrl` (string): URL to recording
- `TranscriptUrl` (string): URL to transcript
- `Notes` (string): Moderator notes
- `CompletedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `Community`
- Belongs to one `User` (participant)
- Has one `Moderator` (User)
- Has many `Consent` records

**Business Rules**:
- Must obtain explicit consent before recording
- Recording must be stored securely (separate container)
- Transcripts must be reviewed for accuracy before analysis

**Aggregate Root**: Yes

---

### FocusGroup

**Description**: Group research session with multiple participants.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `Title` (string): Focus group topic
- `Description` (string): Discussion guide
- `ScheduledAt` (DateTime): Session time
- `Duration` (int): Expected duration in minutes
- `MaxParticipants` (int): Size limit
- `Status` (enum): Scheduled, InProgress, Completed, Cancelled
- `MeetingUrl` (string): Video call link
- `RecordingUrl` (string): URL to recording
- `TranscriptUrl` (string): URL to transcript
- `ModeratorId` (Guid): Lead moderator
- `CompletedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `Community`
- Has many `FocusGroupParticipants`
- Has one `Moderator` (User)
- Has many `Consent` records

**Business Rules**:
- Minimum 3 participants required
- All participants must consent to recording
- Cannot start session before all participants join

**Aggregate Root**: Yes

---

### FocusGroupParticipant

**Description**: Junction entity linking participants to focus group sessions.

**Attributes**:
- `Id` (Guid): Unique identifier
- `FocusGroupId` (Guid): Foreign key to FocusGroup
- `ParticipantId` (Guid): Foreign key to User
- `InvitedAt` (DateTime)
- `ConfirmedAt` (DateTime)
- `JoinedAt` (DateTime): When they entered the call
- `LeftAt` (DateTime): When they left the call
- `AttendanceStatus` (enum): Confirmed, NoShow, Attended

**Relationships**:
- Belongs to one `FocusGroup`
- Belongs to one `User` (participant)

**Business Rules**:
- Participant can only be invited once per focus group
- No-show participants are flagged for follow-up

**Aggregate Root**: No (part of FocusGroup aggregate)

---

### InsightStory

**Description**: Narrative-driven research deliverable combining quotes, media, and statistics.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Foreign key to Community
- `Title` (string): Story headline
- `Subtitle` (string): Summary line
- `AuthorId` (Guid): Analyst who created story
- `Status` (enum): Draft, Review, Approved, Published
- `Template` (string): Story format (ExecutiveSummary, FullReport, VideoReel)
- `PublishedAt` (DateTime)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `Community`
- Has many `StorySection`
- Has one `Author` (User)

**Business Rules**:
- Published stories are immutable (must version for changes)
- Stories must include at least one quote or media element
- Draft stories auto-save every 30 seconds

**Aggregate Root**: Yes

---

### StorySection

**Description**: Individual section within an Insight Story (text, quote, chart, media).

**Attributes**:
- `Id` (Guid): Unique identifier
- `InsightStoryId` (Guid): Foreign key to InsightStory
- `Order` (int): Display order
- `SectionType` (enum): Text, Quote, Chart, Media, Heading
- `Content` (JSON): Section-specific content
  - For Quote: `{ quoteId, attribution, theme }`
  - For Chart: `{ chartType, dataQuery, config }`
  - For Media: `{ mediaUrl, caption, thumbnail }`
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `InsightStory`

**Business Rules**:
- Sections are ordered sequentially
- Cannot delete sections (archive only)
- Quote sections must reference valid quotes

**Aggregate Root**: No (part of InsightStory aggregate)

---

### Consent

**Description**: Participant consent record for research activities.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `UserId` (Guid): Participant who provided consent
- `StudyId` (Guid): Research study or community
- `ConsentType` (enum): GeneralResearch, VideoRecording, DataRetention, SensitiveData
- `Granted` (bool): Whether consent was given
- `ConsentText` (string): Full consent form text shown to participant
- `FormVersion` (string): Version of consent form
- `GrantedAt` (DateTime)
- `WithdrawnAt` (DateTime)
- `IpAddress` (string): IP address at time of consent
- `UserAgent` (string): Browser/device info

**Relationships**:
- Belongs to one `Tenant`
- Belongs to one `User` (participant)

**Business Rules**:
- Consent must be explicit (opt-in, not opt-out)
- Consent can be withdrawn at any time
- Withdrawn consent triggers data deletion workflow
- Minors require parental consent

**Aggregate Root**: Yes

---

### Theme

**Description**: Qualitative coding theme used in analysis.

**Attributes**:
- `Id` (Guid): Unique identifier
- `TenantId` (Guid): Foreign key to Tenant
- `CommunityId` (Guid): Optional, if community-specific
- `Name` (string): Theme label (e.g., "Price Sensitivity")
- `Description` (string): Definition of theme
- `ParentThemeId` (Guid): For hierarchical themes
- `Color` (string): Hex color for visualization
- `CreatedBy` (Guid): Analyst who created theme
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `Tenant`
- Optionally belongs to one `Community`
- Has many `CodedSegments`
- Has one parent `Theme` (optional)

**Business Rules**:
- Theme names must be unique within a community
- Cannot delete theme with coded segments (must archive)
- Max 5 levels of theme hierarchy

**Aggregate Root**: Yes

---

### CodedSegment

**Description**: Text or media segment tagged with a qualitative theme.

**Attributes**:
- `Id` (Guid): Unique identifier
- `ThemeId` (Guid): Foreign key to Theme
- `SourceType` (enum): SurveyResponse, Post, Comment, TaskSubmission, InterviewTranscript
- `SourceId` (Guid): ID of source entity
- `StartPosition` (int): Character offset for text, or timestamp for media
- `EndPosition` (int): End offset
- `SegmentText` (string): Excerpt of text (denormalized for quick access)
- `Sentiment` (enum): Positive, Neutral, Negative
- `CodedBy` (Guid): Analyst who applied code
- `CodedAt` (DateTime)

**Relationships**:
- Belongs to one `Theme`

**Business Rules**:
- Segments can have multiple codes (many-to-many via junction)
- Inter-coder reliability tracked for multi-coder projects
- Segments are immutable once published in Insight Story

**Aggregate Root**: No (part of Theme aggregate)

---

### MediaAnnotation

**Description**: Markup on images or videos (highlights, tags, comments).

**Attributes**:
- `Id` (Guid): Unique identifier
- `TaskSubmissionId` (Guid): Foreign key to TaskSubmission
- `AnnotationType` (enum): Highlight, Tag, Comment, FaceBlur
- `CoordinatesX` (float): X position (percentage)
- `CoordinatesY` (float): Y position (percentage)
- `Width` (float): Annotation width (percentage)
- `Height` (float): Annotation height (percentage)
- `Timestamp` (int): For video annotations (seconds)
- `Label` (string): Tag or comment text
- `CreatedBy` (Guid): User who created annotation
- `CreatedAt` (DateTime)

**Relationships**:
- Belongs to one `TaskSubmission`

**Business Rules**:
- Annotations do not modify original media
- FaceBlur annotations trigger automated redaction
- Cannot annotate rejected submissions

**Aggregate Root**: No (part of TaskSubmission aggregate)

---

## Data Consistency

**Strongly Consistent**:
- User identity and authentication
- Membership and authorization
- Financial transactions (billing)

**Eventually Consistent**:
- Cached counters (view count, reaction count)
- Feed generation and ranking
- Analytics and reporting

**Compensation Strategies**:
- Cached counts recalculated nightly
- Stale data flagged in UI with refresh option
- Background jobs reconcile inconsistencies

