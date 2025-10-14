# Frontend Architecture

## SPA Architecture Overview

Modern single-page application built with React 18+ (or Angular 17+) and TypeScript. The frontend provides a responsive, accessible interface for community engagement, research tools, and administrative functions.

**Core Principles**:
- Component-based architecture with clear separation of concerns
- Type-safe development with TypeScript
- Progressive enhancement for accessibility
- Per-tenant theming and white-labeling
- Performance optimization for mobile and low-bandwidth users
- Offline-capable for critical functions

## Technology Stack

### Core Framework
- **React 18+** with TypeScript (primary recommendation)
  - Functional components with hooks
  - Suspense for code splitting
  - Concurrent rendering for smooth UX
- **Alternative**: Angular 17+ with TypeScript
  - Standalone components
  - Signals for reactive state
  - Angular CLI for tooling

### State Management
- **Redux Toolkit** (for complex state) or **Zustand** (for simpler apps)
- RTK Query for API caching and synchronization
- React Query as alternative for server state
- Local component state for UI-only concerns

### UI Components
- **Material-UI (MUI)** or **Ant Design** for base component library
- Custom components for brand-differentiated features
- Styled-components or CSS Modules for styling
- CSS-in-JS with theme provider for per-tenant theming

### Routing
- React Router v6+ or TanStack Router
- Protected routes with authentication guards
- Role-based route rendering
- Deep linking support for shareable content

### Real-Time Communication
- SignalR client for push notifications and live updates
- WebSocket fallback for real-time features
- Polling as final fallback for constrained environments

### Build and Tooling
- **Vite** (recommended) or Webpack for bundling
- ESLint and Prettier for code quality
- Vitest or Jest for unit testing
- React Testing Library for component testing
- Playwright or Cypress for E2E testing

## Component Hierarchy

### Public Components (Unauthenticated)

**LandingPage**
- Marketing content (if not using separate site)
- Tenant branding
- Login/signup entry points

**AuthComponents**
- `LoginForm`: Email/password or SSO
- `SignupForm`: Registration with email verification
- `ForgotPasswordForm`: Password reset flow
- `SSOCallback`: Handle OAuth redirects

### Member Portal (Authenticated Members)

**AppShell**
- Navigation header with tenant branding
- User menu with profile and settings
- Notification bell with dropdown
- Search bar for content discovery
- Responsive sidebar for mobile

**Feed**
- `FeedContainer`: Main content stream
- `PostCard`: Individual post with reactions
- `InfiniteScroll`: Lazy loading of content
- `FilterBar`: Sort and filter controls

**Post Detail**
- `PostView`: Full post with media
- `CommentThread`: Nested comments
- `ReactionBar`: Like, upvote, sentiment indicators
- `ShareButton`: Copy link or share to external platforms

**Discussions**
- `DiscussionList`: Threaded discussion topics
- `DiscussionThread`: Full conversation view
- `ReplyComposer`: Rich text editor for responses

**Polls**
- `PollCard`: Embedded poll in feed
- `PollDetail`: Full poll view with results
- `VoteInterface`: Radio buttons or checkboxes
- `ResultsChart`: Visual representation of votes

**Member Directory**
- `MemberList`: Searchable, filterable member list
- `MemberCard`: Profile preview with avatar
- `MemberProfile`: Full profile view
- `ConnectionButton`: Follow or connect actions

**User Profile**
- `ProfileHeader`: Avatar, name, bio
- `ActivityFeed`: User's posts and comments
- `ProfileSettings`: Edit profile information
- `NotificationPreferences`: Channel and frequency settings

**Surveys & Research Tasks**
- `ResearchActivityFeed`: Personalized task list (surveys, diaries, interviews)
- `SurveyList`: Available surveys with completion status
- `SurveyTaker`: Multi-step survey interface with progress tracking
- `QuestionRenderer`: Dynamic question types (text, choice, scale, matrix, MaxDiff)
- `VideoDiaryCapture`: Record video responses with prompts
- `PhotoDiaryUpload`: Upload photos with captions and annotations
- `ImageAnnotation`: Markup tools for highlighting/commenting on images
- `CollageBuilder`: Drag-and-drop interface for creating visual collages
- `ConsentForm`: Study-specific consent with signature capture
- `IncentiveTracker`: Points balance and reward catalog

**In-Depth Interview (IDI) Components**
- `InterviewScheduler`: Calendar view for booking interview slots
- `InterviewPrep`: Study materials and consent forms
- `VideoCallInterface`: Embedded video call (Zoom/Teams integration or custom)
- `InterviewRecording`: Recording controls with participant consent

**Focus Group Components**
- `FocusGroupLobby`: Waiting room with participant list
- `GroupDiscussionView`: Multi-participant video grid + chat
- `GroupPolls`: Quick in-session polls for participants
- `RecordingIndicator`: Visual indicator when session is being recorded

### Moderation Console (Moderators / Facilitators)

**ModerationQueue**
- `ContentReviewQueue`: Pending submissions awaiting approval
- `SubmissionCard`: Diary entry, photo, video with moderation actions
- `ModerationActions`: Approve, reject, request revision, ask follow-up
- `QualityScoring`: Rate submission on depth, relevance, authenticity
- `ModerationHistory`: Audit trail of all moderation decisions
- `AutoFlaggedContent`: AI-detected issues (profanity, PII, spam)

**Facilitation Tools**
- `PromptLibrary`: Pre-written follow-up questions and prompts
- `FollowUpComposer`: Send clarifying questions to participants
- `ParticipantList`: View engagement levels and quality scores
- `ActivityMonitoring`: Real-time dashboard of submissions and response rates

**ParticipantManagement**
- `ParticipantList`: All community participants with engagement metrics
- `ParticipantActions`: Suspend, remove, adjust incentive points
- `ParticipantProfile`: Detailed view of contributions and quality scores
- `RecruitmentTracking`: Participant acquisition sources and demographics

### Insight Workspace (Client Analysts / Research Managers)

**Qualitative Coding Interface**
- `CodingWorkspace`: Main interface for tagging responses
- `ResponseViewer`: Display text responses, transcripts, media with timestamps
- `ThemeManager`: Create, edit, merge, nest coding themes
- `TaggingTools`: Apply codes to text segments, video clips, images
- `MultiCoder Support`: Multiple analysts code simultaneously, resolve conflicts
- `InterCoderReliability`: Calculate agreement scores (Cohen's Kappa)

**Quote & Media Management**
- `QuoteLibrary`: Collection of selected impactful quotes
- `MediaClipSelector`: Trim video/audio to specific segments
- `AttributionControl`: Show participant name or anonymize ("Female, 35-44")
- `OrganizeByTheme`: Group quotes and clips by coding themes

**Insight Story Builder**
- `StoryCanvas`: Drag-and-drop narrative builder
- `TemplateLibrary`: Pre-built report templates (Executive Summary, Full Report)
- `ChartInsertion`: Embed survey charts and visualizations
- `QuoteInsertion`: Add formatted participant quotes with attribution
- `MediaEmbedding`: Insert photo/video clips with captions
- `CollaborativeEditing`: Multiple analysts can edit story simultaneously
- `Export Options`: Generate PowerPoint, PDF, Word, video reel

**Mixed-Method Integration**
- `SurveyDataView`: Quantitative results with filtering
- `CrossTab Builder`: Create cross-tabulations of survey data
- `QualQuant Merge`: Display qual themes alongside quant segments
- `StatisticalSignificance`: Highlight significant differences in survey data

**Sentiment & Theme Analytics**
- `SentimentDashboard`: Distribution of positive/negative/neutral/mixed
- `ThemeFrequency`: How often each theme appears across responses
- `ThemeCoOccurrence`: Which themes appear together
- `AIThemeSuggestions`: Azure Cognitive Services suggest emerging themes

### Admin Dashboard (Client Admins / Brand Administrators)

**DashboardHome**
- `MetricsOverview`: Key KPIs
- `RecentActivity`: Latest posts and surveys
- `QuickActions`: Common tasks

**CommunityManagement**
- `CommunityList`: All communities in tenant
- `CommunityEditor`: Create/edit community settings
- `GroupManagement`: Sub-groups and segments
- `MembershipManagement`: Invite and manage members

**ContentManagement**
- `ContentLibrary`: All tenant content
- `ContentEditor`: Rich text editor for announcements
- `MediaManager`: Upload and organize media assets

**SurveyBuilder**
- `SurveyDesigner`: Drag-and-drop question builder
- `QuestionTypes`: Text, multiple choice, scale, matrix, etc.
- `LogicBuilder`: Branching and skip logic
- `SurveySettings`: Timing, visibility, anonymity

**AnalyticsDashboard**
- `CustomDashboards`: Configurable widgets
- `EngagementReports`: Participation metrics
- `SurveyResults`: Response aggregation and visualization
- `ExportTools`: CSV, Excel, PDF exports

**TenantSettings**
- `BrandingEditor`: Logo, colors, fonts
- `FeatureToggles`: Enable/disable features
- `IntegrationConfig`: API keys and webhooks
- `UserRoleManagement`: RBAC configuration

## State Management Strategy

### Global State (Redux Toolkit)

**Slices**:
- `authSlice`: User authentication state, tokens, current user
- `tenantSlice`: Tenant configuration, branding, feature flags
- `feedSlice`: Paginated feed data, filters
- `notificationSlice`: Unread count, notification list
- `uiSlice`: Modals, sidebars, loading states

**Async Actions (RTK Query)**:
```typescript
const api = createApi({
  baseQuery: fetchBaseQuery({
    baseUrl: '/api/v1',
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.token;
      if (token) headers.set('authorization', `Bearer ${token}`);
      return headers;
    }
  }),
  endpoints: (builder) => ({
    getFeed: builder.query<Post[], FeedParams>({
      query: (params) => ({ url: '/posts', params }),
      providesTags: ['Feed']
    }),
    createPost: builder.mutation<Post, CreatePostDto>({
      query: (post) => ({ url: '/posts', method: 'POST', body: post }),
      invalidatesTags: ['Feed']
    })
  })
});
```

### Server State (React Query Alternative)

```typescript
const { data, isLoading } = useQuery({
  queryKey: ['posts', { communityId, page }],
  queryFn: () => fetchPosts(communityId, page),
  staleTime: 5 * 60 * 1000, // 5 minutes
  gcTime: 10 * 60 * 1000
});
```

### Local Component State

Use `useState` and `useReducer` for:
- Form inputs and validation
- UI toggles (dropdown open/closed)
- Temporary component-level data

### Context API

Use for cross-cutting concerns without Redux overhead:
- Theme context (current theme variables)
- Feature flag context (enabled features)
- Tenant context (current tenant info)

## Authentication Flow

### Login Flow
1. User submits credentials via `LoginForm`
2. POST to `/api/v1/auth/login`
3. Receive JWT access token and refresh token
4. Store tokens in httpOnly cookies or memory + localStorage
5. Update Redux `authSlice` with user info
6. Redirect to feed or intended destination

### SSO Flow (OAuth 2.0 + PKCE)
1. User clicks "Login with SSO"
2. Generate code verifier and challenge
3. Redirect to authorization endpoint with challenge
4. User authenticates with identity provider
5. Redirect back to callback URL with auth code
6. Exchange code + verifier for tokens
7. Store tokens and update state

### Token Refresh
```typescript
const refreshAccessToken = async () => {
  const response = await fetch('/api/v1/auth/refresh', {
    method: 'POST',
    credentials: 'include' // Send refresh token cookie
  });
  const { accessToken } = await response.json();
  dispatch(setAccessToken(accessToken));
};
```

### Protected Routes
```typescript
const ProtectedRoute = ({ children, allowedRoles }) => {
  const { user, isAuthenticated } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" />;
  }
  
  if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
    return <Navigate to="/unauthorized" />;
  }
  
  return children;
};
```

## Role-Based Rendering

### Route-Level Authorization
```typescript
<Routes>
  <Route path="/feed" element={<ProtectedRoute><Feed /></ProtectedRoute>} />
  <Route 
    path="/moderation" 
    element={
      <ProtectedRoute allowedRoles={['Moderator', 'Admin']}>
        <ModerationConsole />
      </ProtectedRoute>
    } 
  />
  <Route 
    path="/admin" 
    element={
      <ProtectedRoute allowedRoles={['Admin']}>
        <AdminDashboard />
      </ProtectedRoute>
    } 
  />
</Routes>
```

### Component-Level Authorization
```typescript
const PostCard = ({ post }) => {
  const { hasPermission } = useAuth();
  
  return (
    <Card>
      <PostContent content={post.content} />
      {hasPermission('post.delete') && (
        <DeleteButton postId={post.id} />
      )}
    </Card>
  );
};
```

### Custom Hook
```typescript
const useAuth = () => {
  const { user } = useSelector((state) => state.auth);
  
  const hasRole = (role: string) => user?.roles.includes(role);
  const hasPermission = (permission: string) => 
    user?.permissions.includes(permission);
  
  return { user, hasRole, hasPermission };
};
```

## Theming and White-Labeling

### Theme Structure
```typescript
interface TenantTheme {
  colors: {
    primary: string;
    secondary: string;
    background: string;
    surface: string;
    text: string;
    error: string;
    warning: string;
    success: string;
  };
  typography: {
    fontFamily: string;
    fontSize: number;
    fontWeightLight: number;
    fontWeightRegular: number;
    fontWeightBold: number;
  };
  spacing: number;
  borderRadius: number;
  logo: string; // URL to tenant logo
  favicon: string;
}
```

### Theme Provider
```typescript
const ThemeProvider = ({ children }) => {
  const { tenant } = useSelector((state) => state.tenant);
  const theme = createTheme(tenant.theme);
  
  return (
    <MuiThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </MuiThemeProvider>
  );
};
```

### Dynamic Styling
```typescript
const StyledButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  '&:hover': {
    backgroundColor: theme.palette.primary.dark
  }
}));
```

### Runtime Theme Loading
```typescript
useEffect(() => {
  const loadTenantTheme = async () => {
    const theme = await fetchTenantTheme(tenantId);
    dispatch(setTheme(theme));
    
    // Update CSS variables for non-component styling
    document.documentElement.style.setProperty('--primary-color', theme.colors.primary);
    document.documentElement.style.setProperty('--font-family', theme.typography.fontFamily);
  };
  
  loadTenantTheme();
}, [tenantId]);
```

## Accessibility

### WCAG 2.1 AA Compliance
- Color contrast ratios minimum 4.5:1 for text
- Focus indicators on all interactive elements
- Keyboard navigation support
- Screen reader compatibility

### Semantic HTML
```typescript
const PostCard = ({ post }) => (
  <article aria-labelledby={`post-${post.id}-title`}>
    <header>
      <h2 id={`post-${post.id}-title`}>{post.title}</h2>
    </header>
    <div role="main">
      {post.content}
    </div>
    <footer>
      <button aria-label={`Like post ${post.title}`}>Like</button>
    </footer>
  </article>
);
```

### ARIA Attributes
- `aria-label` for icon-only buttons
- `aria-describedby` for form validation messages
- `aria-live` regions for dynamic content updates
- `aria-expanded` for collapsible sections

### Keyboard Navigation
- Tab order follows visual hierarchy
- Escape key closes modals and dropdowns
- Arrow keys navigate lists and menus
- Enter/Space activate buttons and links

## Internationalization (i18n)

### Library
- `react-i18next` or `react-intl`

### Translation Files
```typescript
// en.json
{
  "feed.title": "Community Feed",
  "post.like": "Like",
  "post.comment": "Comment",
  "survey.submit": "Submit Survey"
}

// es.json
{
  "feed.title": "Feed de la Comunidad",
  "post.like": "Me gusta",
  "post.comment": "Comentar",
  "survey.submit": "Enviar Encuesta"
}
```

### Usage
```typescript
import { useTranslation } from 'react-i18next';

const Feed = () => {
  const { t } = useTranslation();
  
  return (
    <div>
      <h1>{t('feed.title')}</h1>
      <Button>{t('post.like')}</Button>
    </div>
  );
};
```

### Date/Number Formatting
```typescript
const formatDate = (date: Date, locale: string) => {
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date);
};
```

## API Integration

### HTTP Client (Axios)
```typescript
const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Request interceptor for auth token
apiClient.interceptors.request.use((config) => {
  const token = store.getState().auth.token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor for token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      await refreshAccessToken();
      return apiClient.request(error.config);
    }
    return Promise.reject(error);
  }
);
```

### GraphQL (Future)
```typescript
const client = new ApolloClient({
  uri: '/graphql',
  cache: new InMemoryCache(),
  link: new HttpLink({
    uri: '/graphql',
    headers: {
      authorization: `Bearer ${token}`
    }
  })
});

const GET_FEED = gql`
  query GetFeed($communityId: ID!, $limit: Int!) {
    posts(communityId: $communityId, limit: $limit) {
      id
      title
      content
      author {
        id
        name
        avatar
      }
      reactions {
        type
        count
      }
    }
  }
`;
```

### Real-Time (SignalR)
```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => store.getState().auth.token
  })
  .withAutomaticReconnect()
  .build();

connection.on('NewPost', (post) => {
  dispatch(addPostToFeed(post));
});

connection.on('NewNotification', (notification) => {
  dispatch(addNotification(notification));
});

await connection.start();
```

## Caching Strategy

### API Response Caching
- RTK Query automatic caching with tag-based invalidation
- Stale-while-revalidate pattern for non-critical data
- Cache busting on mutations

### Local Storage
- User preferences (theme, language, display settings)
- Draft posts and survey responses
- Recently viewed communities

### Service Worker (PWA)
- Cache static assets (JS, CSS, images)
- Offline-first strategy for core functionality
- Background sync for posting when offline

## Performance Optimization

### Code Splitting
```typescript
const AdminDashboard = lazy(() => import('./pages/AdminDashboard'));
const ModerationConsole = lazy(() => import('./pages/ModerationConsole'));

<Suspense fallback={<LoadingSpinner />}>
  <AdminDashboard />
</Suspense>
```

### Image Optimization
- Lazy loading with `loading="lazy"` or Intersection Observer
- Responsive images with srcset
- WebP format with fallbacks
- CDN delivery for media assets

### Virtualization
```typescript
import { FixedSizeList } from 'react-window';

const MemberList = ({ members }) => (
  <FixedSizeList
    height={600}
    itemCount={members.length}
    itemSize={80}
  >
    {({ index, style }) => (
      <div style={style}>
        <MemberCard member={members[index]} />
      </div>
    )}
  </FixedSizeList>
);
```

### Memoization
```typescript
const PostCard = memo(({ post }) => {
  // Component only re-renders if post changes
});

const filteredPosts = useMemo(() => {
  return posts.filter(post => post.communityId === selectedCommunity);
}, [posts, selectedCommunity]);
```

### Debouncing
```typescript
const SearchBar = () => {
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebounce(query, 500);
  
  useEffect(() => {
    if (debouncedQuery) {
      dispatch(searchPosts(debouncedQuery));
    }
  }, [debouncedQuery]);
  
  return <input onChange={(e) => setQuery(e.target.value)} />;
};
```

## Error Handling

### Error Boundaries
```typescript
class ErrorBoundary extends React.Component {
  componentDidCatch(error, errorInfo) {
    logErrorToService(error, errorInfo);
  }
  
  render() {
    if (this.state.hasError) {
      return <ErrorFallback />;
    }
    return this.props.children;
  }
}
```

### API Error Handling
```typescript
const handleApiError = (error: AxiosError) => {
  if (error.response) {
    // Server responded with error status
    switch (error.response.status) {
      case 401:
        dispatch(logout());
        navigate('/login');
        break;
      case 403:
        showToast('You do not have permission', 'error');
        break;
      case 404:
        showToast('Resource not found', 'error');
        break;
      default:
        showToast('An error occurred', 'error');
    }
  } else if (error.request) {
    // Request made but no response
    showToast('Network error. Please check your connection.', 'error');
  }
};
```

## Testing Strategy

### Unit Tests (Vitest)
```typescript
describe('PostCard', () => {
  it('renders post title and content', () => {
    const post = { id: '1', title: 'Test', content: 'Content' };
    render(<PostCard post={post} />);
    expect(screen.getByText('Test')).toBeInTheDocument();
  });
  
  it('shows delete button for post author', () => {
    const user = { id: '1', roles: ['Member'] };
    render(<PostCard post={post} />, { user });
    expect(screen.getByRole('button', { name: /delete/i })).toBeVisible();
  });
});
```

### Integration Tests (React Testing Library)
```typescript
it('creates a new post', async () => {
  render(<Feed />);
  
  const input = screen.getByPlaceholderText('What\'s on your mind?');
  await userEvent.type(input, 'New post content');
  
  const submitButton = screen.getByRole('button', { name: /post/i });
  await userEvent.click(submitButton);
  
  await waitFor(() => {
    expect(screen.getByText('New post content')).toBeInTheDocument();
  });
});
```

### E2E Tests (Playwright)
```typescript
test('user can complete survey', async ({ page }) => {
  await page.goto('/surveys/123');
  
  await page.fill('[data-testid="question-1"]', 'Answer 1');
  await page.click('[data-testid="option-2"]');
  await page.click('button:has-text("Next")');
  
  await page.fill('[data-testid="question-3"]', 'Final answer');
  await page.click('button:has-text("Submit")');
  
  await expect(page.locator('text=Thank you')).toBeVisible();
});
```

## Build and Deployment

### Environment Variables
```
REACT_APP_API_URL=https://api.platform.com
REACT_APP_ENVIRONMENT=production
REACT_APP_SENTRY_DSN=https://...
REACT_APP_SIGNALR_URL=https://api.platform.com/hubs
```

### Build Optimization
- Tree shaking for unused code
- Minification and compression
- Source maps for production debugging
- Bundle analysis (webpack-bundle-analyzer)

### Deployment Targets
- Azure Static Web Apps or App Service
- CDN distribution (Azure Front Door)
- SSL/TLS with automatic certificate renewal
- Blue-green deployments for zero-downtime

