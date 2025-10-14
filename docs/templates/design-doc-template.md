# [Feature Name] - Design Document

**Status**: [Draft | In Review | Approved | Implemented]
**Author**: [Name]
**Created**: [Date]
**Last Updated**: [Date]
**Reviewers**: [Names]
**Related Epics**: [Jira Epic IDs]

---

## Executive Summary

A 2-3 sentence overview of what this feature does and why it matters.

**Example**:
> The Qualitative Coding Workspace enables research analysts to tag responses with themes, calculate inter-coder reliability, and organize quotes for insight stories. This feature is critical for transforming raw qualitative data into structured insights that can be merged with quantitative survey results.

---

## Problem Statement

### Current State
What is the current situation? What pain points exist?

### Desired State  
What should the experience be after this feature is implemented?

### User Impact
Who is affected and how? Quantify if possible.

**Example**:
```
Current State:
Analysts manually tag responses in spreadsheets, losing context and making 
collaboration difficult. Inter-coder reliability cannot be calculated systematically.

Desired State:
Analysts work in a dedicated coding interface where they can apply themes to 
responses, see what colleagues have coded, resolve conflicts, and automatically 
calculate reliability metrics.

User Impact:
- 50+ research analysts across 10 brands
- Reduces coding time by 40% (from manual process)
- Enables real-time collaboration on large studies
```

---

## Goals and Non-Goals

### Goals
What are we trying to achieve?
- Goal 1
- Goal 2
- Goal 3

### Non-Goals
What is explicitly out of scope?
- Non-goal 1
- Non-goal 2

**Example**:
```
Goals:
- Enable multiple analysts to code simultaneously
- Calculate Cohen's Kappa for 2-coder projects
- Support hierarchical theme taxonomy (3 levels deep)
- Export coded data to MaxQDA and NVivo

Non-Goals:
- Machine learning auto-coding (future phase)
- Video coding with timestamps (separate feature)
- Support for 5+ levels of theme hierarchy
```

---

## Success Metrics

How will we measure success?

| Metric | Current | Target | Timeframe |
|--------|---------|--------|-----------|
| Coding time per response | 5 min | 3 min | 3 months |
| Inter-coder agreement | Unknown | >75% | Launch |
| Analyst satisfaction | - | 4.5/5 | 6 months |

---

## User Stories

High-level user stories this design addresses:

1. **As a** research analyst **I want to** tag responses with themes **so that** I can organize qualitative data
2. **As a** lead researcher **I want to** see inter-coder reliability **so that** I can trust the coding quality
3. **As an** analyst **I want to** export coded data **so that** I can analyze it in other tools

---

## Proposed Solution

### High-Level Approach

Describe the solution architecture at a high level with diagrams.

```
[Include architecture diagram]

Components:
- Coding Interface (React component)
- Theme Management Service
- Code Application Service
- Reliability Calculator
- Export Service
```

### Key Design Decisions

Document important choices:

**Decision 1**: [Choice made]
- **Why**: [Rationale]
- **Trade-offs**: [Pros and cons]
- **Alternatives considered**: [What else we looked at]

**Example**:
```
Decision: Store coded segments in relational database (SQL) not NoSQL

Why: 
- Need complex queries (filter by theme, coder, sentiment)
- ACID guarantees for reliability calculations
- Easier to ensure data integrity with foreign keys

Trade-offs:
Pros: Strong consistency, complex queries, integrity
Cons: May not scale to millions of segments (acceptable for now)

Alternatives considered:
- Cosmos DB: Better for massive scale but harder to query
- Event sourcing: Too complex for MVP
```

---

## Technical Design

### Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                  Frontend (React)                    │
│  ┌──────────────┐  ┌──────────────┐                │
│  │ Response     │  │ Theme        │                 │
│  │ Viewer       │  │ Manager      │                 │
│  └──────────────┘  └──────────────┘                 │
└─────────────────────────────────────────────────────┘
                        │
                        │ REST API
                        ▼
┌─────────────────────────────────────────────────────┐
│              Backend Services (.NET)                 │
│  ┌──────────────┐  ┌──────────────┐                │
│  │ Coding       │  │ Reliability  │                 │
│  │ Service      │  │ Calculator   │                 │
│  └──────────────┘  └──────────────┘                 │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│            Azure SQL Database                        │
│  Themes | CodedSegments | Responses                 │
└─────────────────────────────────────────────────────┘
```

### Data Model

**New Tables**:

```sql
CREATE TABLE Themes (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    CommunityId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    ParentThemeId UNIQUEIDENTIFIER NULL,
    Color NVARCHAR(7),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ParentThemeId) REFERENCES Themes(Id)
);

CREATE TABLE CodedSegments (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ThemeId UNIQUEIDENTIFIER NOT NULL,
    ResponseId UNIQUEIDENTIFIER NOT NULL,
    StartPosition INT,
    EndPosition INT,
    SegmentText NVARCHAR(MAX),
    Sentiment NVARCHAR(50),
    CodedBy UNIQUEIDENTIFIER NOT NULL,
    CodedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ThemeId) REFERENCES Themes(Id),
    FOREIGN KEY (ResponseId) REFERENCES SurveyResponses(Id)
);

CREATE INDEX IX_CodedSegments_Theme ON CodedSegments(ThemeId);
CREATE INDEX IX_CodedSegments_Response ON CodedSegments(ResponseId);
```

**Modified Tables**: [None | List tables being modified]

### API Endpoints

**Theme Management**:
```
POST   /api/v1/themes                    Create theme
GET    /api/v1/themes                    List themes
GET    /api/v1/themes/{id}               Get theme details
PUT    /api/v1/themes/{id}               Update theme
DELETE /api/v1/themes/{id}               Delete theme
```

**Coding Operations**:
```
POST   /api/v1/coding/apply              Apply code to segment
DELETE /api/v1/coding/{id}               Remove code
GET    /api/v1/coding/response/{id}      Get codes for response
```

**Reliability**:
```
GET    /api/v1/coding/reliability        Calculate inter-coder reliability
```

### State Management (Frontend)

**Redux Slices**:
- `themesSlice` - Theme CRUD and hierarchy
- `codingSlice` - Applied codes and active coding session
- `responsesSlice` - Responses being coded

**Key Actions**:
- `applyCode(responseId, themeId, segment)`
- `removeCode(codeId)`
- `calculateReliability(studyId, coderIds)`

### Business Logic

**Inter-Coder Reliability (Cohen's Kappa)**:
```csharp
public class ReliabilityCalculator
{
    public async Task<ReliabilityResult> CalculateCohenKappa(
        Guid studyId, 
        Guid coder1Id, 
        Guid coder2Id)
    {
        // Get all responses coded by both coders
        var responses = await GetSharedResponses(studyId, coder1Id, coder2Id);
        
        int agreements = 0;
        int total = responses.Count;
        
        foreach (var response in responses)
        {
            var codes1 = await GetCodesForResponse(response.Id, coder1Id);
            var codes2 = await GetCodesForResponse(response.Id, coder2Id);
            
            // Check for theme agreement
            if (codes1.Select(c => c.ThemeId).Intersect(codes2.Select(c => c.ThemeId)).Any())
            {
                agreements++;
            }
        }
        
        double observedAgreement = (double)agreements / total;
        double expectedAgreement = CalculateExpectedAgreement(responses);
        double kappa = (observedAgreement - expectedAgreement) / (1 - expectedAgreement);
        
        return new ReliabilityResult
        {
            Kappa = kappa,
            Interpretation = InterpretKappa(kappa),
            Agreements = agreements,
            Total = total
        };
    }
}
```

### Performance Considerations

**Query Optimization**:
- Index on `(ThemeId, ResponseId)` for fast filtering
- Paginate coded segments (100 per page)
- Cache theme hierarchy in Redis (15-minute TTL)

**Scalability**:
- Current design handles 10,000 responses with 5 codes each (50,000 records)
- For 100,000+ responses, consider partitioning by study ID
- Background job for reliability calculation on large datasets

---

## Security Considerations

**Access Control**:
- Only analysts with `coding.write` permission can apply codes
- Codes are tenant-scoped (can't see other tenant's themes)
- Inter-coder reliability only calculated for same-study coders

**Data Privacy**:
- Coded segments include response text for context
- Export anonymizes participant IDs if configured

---

## Integration Points

**External Systems**:
- MaxQDA export via XML format
- NVivo export via .nvp format
- SPSS export with coded segment indicators

**Internal Services**:
- Survey Response Service (read responses)
- User Service (coder names)
- Export Service (generate exports)

---

## Migration Strategy

**Phase 1: Database Migration**
```bash
# Create new tables
dotnet ef migrations add AddCodingTables

# Deploy to staging
dotnet ef database update --environment Staging

# Verify with smoke tests
```

**Phase 2: Feature Rollout**
- Week 1: Beta to 5 internal analysts
- Week 2: Expand to 20 early-adopter clients
- Week 3: General availability with feature flag
- Week 4: Remove feature flag, mark stable

**Rollback Plan**:
If critical issues arise:
1. Disable feature flag (instant)
2. Keep data tables intact (no data loss)
3. Fix issues and re-enable

---

## Testing Strategy

**Unit Tests**:
- Theme CRUD operations
- Code application logic
- Reliability calculation algorithm

**Integration Tests**:
- End-to-end coding workflow
- Multi-coder scenarios
- Export generation

**User Acceptance Testing**:
- 5 analysts test on real studies
- Feedback collected via survey
- Usability session recordings

**Performance Tests**:
- Load test with 10,000 responses
- Reliability calculation with 1,000 responses
- Concurrent coding by 10 analysts

---

## Open Questions

Track unresolved decisions:

1. **Question**: Should we support undo/redo for code application?
   - **Impact**: Medium - affects UX complexity
   - **Decision needed by**: Design review
   - **Owner**: Frontend lead

2. **Question**: How do we handle deleted themes that have coded segments?
   - **Options**: Soft delete, prevent deletion, orphan codes
   - **Impact**: High - affects data integrity
   - **Decision needed by**: Tech review
   - **Owner**: Backend lead

---

## Alternatives Considered

### Alternative 1: NoSQL (Cosmos DB) for Coded Segments

**Pros**:
- Better scalability for massive datasets
- Flexible schema for future extensions

**Cons**:
- Complex queries harder to write
- No foreign key constraints
- Higher cost at scale

**Why rejected**: SQL is sufficient for expected scale and provides better query capabilities

### Alternative 2: Event Sourcing for Coding Actions

**Pros**:
- Complete audit trail
- Easy undo/redo
- Temporal queries

**Cons**:
- Much higher complexity
- Harder to calculate reliability
- Overkill for MVP

**Why rejected**: Too complex for initial version; can add later if needed

---

## Dependencies

**Blocking Dependencies**:
- THEME-12: Theme taxonomy UI must be complete
- SURVEY-45: Response viewer component must exist

**Non-Blocking Dependencies**:
- EXPORT-23: Export service (can build export later)

---

## Timeline

**Phase 1: Backend Foundation** (Week 1-2)
- Database schema
- API endpoints
- Business logic

**Phase 2: Frontend Interface** (Week 3-4)
- Coding UI components
- Theme management
- Integration with backend

**Phase 3: Advanced Features** (Week 5-6)
- Inter-coder reliability
- Export functionality
- Performance optimization

**Total**: 6 weeks

---

## Post-Launch Plan

**Monitoring**:
- Track coding actions per day
- Monitor API response times
- Alert on reliability calculation failures

**Iteration**:
- Gather user feedback monthly
- Analyze usage patterns
- Plan enhancements for Phase 2

**Future Enhancements**:
- AI-assisted theme suggestions
- Video timestamp coding
- Sentiment analysis integration
- Advanced statistical measures (Krippendorff's Alpha)

---

## Appendix

### References
- [MaxQDA Export Format Spec](https://example.com)
- [Cohen's Kappa Calculation](https://example.com)
- [Related ADR-015: Use SQL for Analytical Data](../architecture/decisions/015-use-sql-for-analytics.md)

### Glossary
- **Inter-coder reliability**: Agreement between multiple coders on the same data
- **Cohen's Kappa**: Statistical measure of agreement adjusted for chance
- **Coding taxonomy**: Hierarchical organization of themes/codes

---

## Change Log

| Date | Author | Changes |
|------|--------|---------|
| 2025-01-15 | Jane Doe | Initial draft |
| 2025-01-18 | Jane Doe | Added reliability calculation details |
| 2025-01-20 | Team | Approved after tech review |

