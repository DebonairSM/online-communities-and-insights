# User Story Template

Use this template when creating user stories in Jira. Copy the structure below into your Jira story description.

---

## Story Title
[Action-oriented, user-focused title]
Example: "Member can upload photos to diary entry"

---

## User Story

**As a** [role/persona]
**I want to** [action/capability]
**So that** [benefit/value]

### Example
**As a** community member
**I want to** upload multiple photos to my diary entry
**So that** I can share visual context about my experiences

---

## Acceptance Criteria

Use checkbox format for clear, testable criteria:

- [ ] **Given** [context/precondition]
      **When** [action]
      **Then** [expected outcome]

### Example Acceptance Criteria

- [ ] Given I am on the diary submission screen
      When I click "Add Photos"
      Then I can select up to 10 images from my device

- [ ] Given I have selected photos
      When I submit the diary entry
      Then all photos are uploaded successfully

- [ ] Given photos are uploading
      When I navigate away
      Then upload continues in background

- [ ] Given upload fails due to network
      When connection is restored
      Then upload automatically retries

---

## Technical Notes

Brief implementation details for developers:

### Backend
- API endpoints affected
- Database schema changes
- Business logic considerations

### Frontend
- Components to create/modify
- State management approach
- API integration points

### Dependencies
- Related stories
- External services
- Third-party libraries

### Example
```
Backend:
- POST /api/v1/diaries/{id}/photos (multipart/form-data)
- Add Photos table with diary_id FK
- Implement chunked upload for large files

Frontend:
- Create PhotoUploadComponent (React)
- Use Redux for upload progress tracking
- Integrate with existing DiaryForm

Dependencies:
- Requires Azure Blob Storage configuration
- Depends on FOUND-18 (Storage Account setup)
```

---

## Testing Notes

Specific scenarios to test beyond acceptance criteria:

- [ ] Test with various image formats (JPEG, PNG, GIF, HEIC)
- [ ] Test with very large images (>10MB)
- [ ] Test with poor network connection
- [ ] Test upload cancellation
- [ ] Test error handling and user feedback
- [ ] Test on mobile devices (iOS, Android)

---

## Design Reference

Link to Figma, mockups, or design documents:
- Design: [Figma link]
- Flows: [Miro board link]
- Related ADR: [Link to ADR if applicable]

---

## Definition of Done

Standard checklist (customize per team):

- [ ] Code complete and peer reviewed
- [ ] Unit tests written (min 80% coverage for new code)
- [ ] Integration tests pass
- [ ] API documented in Swagger (if backend changes)
- [ ] Component documented in Storybook (if frontend changes)
- [ ] Tested in dev environment
- [ ] Security review completed (if handling sensitive data)
- [ ] Performance acceptable (meets SLA)
- [ ] Accessibility standards met (WCAG 2.1 AA)
- [ ] No new linter errors
- [ ] Database migration tested (if schema changes)
- [ ] Feature flag created (if applicable)
- [ ] Product owner acceptance

---

## Story Points

Use Fibonacci sequence: 1, 2, 3, 5, 8, 13, 21

**Guidelines**:
- **1 point**: Simple change, < 2 hours (e.g., text change, config update)
- **2 points**: Small feature, < 1 day (e.g., simple CRUD endpoint)
- **3 points**: Medium feature, 1-2 days (e.g., new component with state)
- **5 points**: Complex feature, 2-3 days (e.g., multi-step form with validation)
- **8 points**: Large feature, 3-5 days (e.g., new page with multiple components)
- **13 points**: Very large, 1 week+ (consider breaking down)
- **21+ points**: Epic-sized, definitely break down into smaller stories

---

## Labels

Apply relevant labels for filtering and organization:

- **Type**: `frontend`, `backend`, `database`, `api`, `mobile`, `devops`
- **Priority**: `critical`, `high`, `medium`, `low`
- **Component**: `auth`, `community`, `survey`, `moderation`, `analytics`
- **Status**: `blocked`, `needs-design`, `needs-review`, `technical-debt`

---

## Example Complete Story

```
Title: Member can upload photos to diary entry

Story:
As a community member
I want to upload multiple photos to my diary entry
So that I can share visual context about my experiences

Acceptance Criteria:
- [ ] Given I am on diary submission screen, when I click "Add Photos", then I can select up to 10 images
- [ ] Given I have selected photos, when I submit the diary, then all photos upload successfully with progress indicator
- [ ] Given photos are uploading, when I navigate away, then upload continues in background
- [ ] Given upload fails, when connection restores, then upload automatically retries up to 3 times

Technical Notes:
Backend:
- POST /api/v1/diaries/{id}/photos
- Add Photos table (id, diary_id, url, order, created_at)
- Chunked upload for files >10MB
- Generate thumbnails on upload

Frontend:
- Create PhotoUploadComponent with drag-drop
- Redux slice for upload queue management
- Show upload progress per photo

Dependencies:
- FOUND-18 (Azure Blob Storage)
- DIARY-12 (Diary submission form)

Testing:
- Test JPEG, PNG, GIF, HEIC formats
- Test 50MB image
- Test poor network (throttled)
- Test mobile Safari and Chrome

Story Points: 5
Labels: frontend, backend, research-tools, high-priority
```

