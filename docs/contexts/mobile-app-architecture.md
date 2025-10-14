# Mobile App Architecture

## Overview

The mobile application is a critical component of the Insight Community Platform, enabling participants to complete research tasks in-situ with photo/video capture, offline participation, and push notifications. The app is designed for research participants, not moderators or analysts (who use the web application).

**Target Users**: Research participants capturing authentic, contextual insights

**Key Use Cases**:
- Photo/video diaries captured in real-world moments
- Surveys completed on-the-go with offline capability
- Push notifications for new research activities
- In-store product evaluations with mobile capture
- Daily journal entries while experiences are fresh

## Technology Approach

### Cross-Platform Framework

**Recommended**: React Native

**Rationale**:
- Code sharing with web frontend (React + TypeScript)
- Shared Redux state management logic
- Large ecosystem and mature tooling
- Good performance for media-heavy applications
- Hot reload for rapid development
- Strong community and enterprise adoption

**Alternative**: Flutter

**Considerations**:
- Better performance for complex animations
- More consistent UI across platforms
- Smaller community than React Native
- Dart language requires additional expertise

### Native Modules

Certain features require native iOS/Android code:
- Camera and microphone access with custom controls
- Background upload of large video files
- Push notifications
- Offline data encryption
- Biometric authentication (Face ID, fingerprint)

## Architecture Components

### Navigation Structure

```
AppStack
├── AuthStack (if not logged in)
│   ├── LoginScreen
│   ├── SignupScreen
│   └── ConsentOnboardingScreen
└── MainTabs (bottom navigation)
    ├── ActivityTab
    │   ├── ActivityFeedScreen
    │   ├── TaskDetailScreen
    │   ├── SurveyTakerScreen
    │   ├── VideoDiaryCaptureScreen
    │   ├── PhotoDiaryUploadScreen
    │   └── CollagingScreen
    ├── ProfileTab
    │   ├── ProfileScreen
    │   ├── IncentiveScreen
    │   └── SettingsScreen
    └── NotificationsTab
        └── NotificationsScreen
```

### State Management

**Redux Toolkit** with persistence:
- `authSlice`: User authentication, tokens
- `tasksSlice`: Research activities with offline queue
- `mediaSlice`: Photo/video uploads with retry queue
- `syncSlice`: Offline sync status and conflicts
- `incentivesSlice`: Points balance and rewards

**Redux Persist**:
- Store auth tokens securely in device keychain
- Cache task data for offline access
- Queue uploads when network unavailable

### Offline-First Design

**SQLite Local Database**:
```sql
CREATE TABLE tasks (
    id TEXT PRIMARY KEY,
    type TEXT, -- 'survey', 'diary', 'photo', 'video'
    title TEXT,
    description TEXT,
    deadline TEXT,
    status TEXT, -- 'available', 'in_progress', 'completed', 'synced'
    data TEXT, -- JSON blob
    created_at INTEGER,
    synced_at INTEGER
);

CREATE TABLE media_uploads (
    id TEXT PRIMARY KEY,
    task_id TEXT,
    file_path TEXT,
    file_type TEXT, -- 'photo', 'video'
    file_size INTEGER,
    upload_status TEXT, -- 'pending', 'uploading', 'completed', 'failed'
    upload_progress REAL,
    retry_count INTEGER,
    error_message TEXT,
    created_at INTEGER
);

CREATE TABLE responses (
    id TEXT PRIMARY KEY,
    task_id TEXT,
    question_id TEXT,
    answer TEXT,
    is_synced INTEGER DEFAULT 0,
    created_at INTEGER
);
```

**Sync Strategy**:
1. Participant completes task while offline
2. Data saved to local SQLite database
3. Media files saved to device storage
4. When network available, background sync begins
5. Upload media files with chunked upload + retry
6. Send response data to API
7. Mark as synced, remove from pending queue

### Media Capture

**Camera Module** (react-native-vision-camera or expo-camera):
```typescript
import { Camera, useCameraDevices } from 'react-native-vision-camera';

const VideoDiaryCapture: React.FC = () => {
  const devices = useCameraDevices();
  const device = devices.back;
  const camera = useRef<Camera>(null);
  
  const startRecording = async () => {
    const video = await camera.current?.startRecording({
      onRecordingFinished: (video) => saveVideoToQueue(video.path),
      onRecordingError: (error) => handleError(error),
    });
  };
  
  return (
    <Camera
      ref={camera}
      device={device}
      video={true}
      audio={true}
    />
  );
};
```

**Photo Capture**:
```typescript
import { launchCamera } from 'react-native-image-picker';

const capturePhoto = async () => {
  const result = await launchCamera({
    mediaType: 'photo',
    quality: 0.8,
    maxWidth: 1920,
    maxHeight: 1080,
    saveToPhotos: false
  });
  
  if (result.assets && result.assets[0]) {
    await savePhotoToQueue(result.assets[0]);
  }
};
```

**Video Transcoding**:
- Compress videos before upload (target 720p, H.264 codec)
- FFmpeg integration for iOS/Android
- Maintain acceptable quality while reducing file size

### Background Upload

**iOS**: Background URLSession
```swift
let config = URLSessionConfiguration.background(withIdentifier: "com.platform.upload")
let session = URLSession(configuration: config, delegate: self, delegateQueue: nil)
```

**Android**: WorkManager
```kotlin
val uploadWorkRequest = OneTimeWorkRequestBuilder<UploadWorker>()
    .setConstraints(
        Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()
    )
    .setBackoffCriteria(BackoffPolicy.EXPONENTIAL, 10, TimeUnit.SECONDS)
    .build()
```

**Upload Strategy**:
- Chunked upload for files > 10MB
- Resume capability for interrupted uploads
- Exponential backoff for failed uploads (1s, 2s, 4s, 8s, 16s, max 5 retries)
- Upload only on Wi-Fi if setting enabled (default: use cellular)

### Push Notifications

**Firebase Cloud Messaging** (cross-platform):
```typescript
import messaging from '@react-native-firebase/messaging';

// Request permission (iOS)
const authStatus = await messaging().requestPermission();

// Handle incoming messages
messaging().onMessage(async remoteMessage => {
  const { title, body, data } = remoteMessage.notification;
  
  // Show local notification
  showNotification(title, body);
  
  // Handle deep link
  if (data?.taskId) {
    navigation.navigate('TaskDetail', { taskId: data.taskId });
  }
});

// Handle background messages
messaging().setBackgroundMessageHandler(async remoteMessage => {
  console.log('Background message:', remoteMessage);
});
```

**Notification Types**:
- New research task available
- Task deadline approaching (24 hours before)
- Response approved by moderator
- Incentive points awarded
- New message from moderator
- Study invitation

### API Integration

**Shared API Client** (with web):
```typescript
import axios from 'axios';
import { getAuthToken } from './auth';

const apiClient = axios.create({
  baseURL: 'https://api.communities.com/v1',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'X-Platform': 'mobile-ios', // or 'mobile-android'
  }
});

// Request interceptor for auth token
apiClient.interceptors.request.use(async (config) => {
  const token = await getAuthToken();
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
      await refreshToken();
      return apiClient.request(error.config);
    }
    return Promise.reject(error);
  }
);
```

**Endpoints Used by Mobile**:
- `GET /tasks` - Fetch available research activities
- `POST /tasks/{id}/responses` - Submit survey responses
- `POST /media/upload` - Upload photo/video with metadata
- `GET /profile` - User profile and incentive balance
- `GET /notifications` - Fetch notification history
- `POST /consent` - Submit consent for new studies

### Permissions Management

**Required Permissions**:

**iOS (Info.plist)**:
```xml
<key>NSCameraUsageDescription</key>
<string>We need camera access to capture photos and videos for research tasks.</string>
<key>NSMicrophoneUsageDescription</key>
<string>We need microphone access to record video diaries.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>We need photo library access to select and upload images.</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>We may collect location for context in certain research tasks (optional).</string>
```

**Android (AndroidManifest.xml)**:
```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.INTERNET" />
```

**Permission Request Flow**:
1. Show explanation screen before requesting
2. Request permission when needed (not all at once)
3. Handle denial gracefully with explanation
4. Provide deep link to app settings if permission previously denied

## Unique Mobile Features

### In-Situ Capture

**Context Metadata**:
- Timestamp of capture
- GPS location (if permitted)
- Device type and OS version
- Network type (Wi-Fi, LTE, 5G)

**Real-World Research**:
- Capture product usage in home
- Document shopping experiences in-store
- Record meal preparation with running commentary
- Photograph purchase decisions at point-of-sale

### Rich Media Tasks

**Video Diary**:
- Guided prompts on-screen during recording
- Front-facing camera for authentic reactions
- Maximum duration limits (e.g., 2 minutes)
- Preview before submission
- Re-record capability

**Photo Annotation**:
- Draw on photos with highlighter/arrow tools
- Add text labels and comments
- Circle/underline areas of interest
- Save annotated version for upload

**Collaging**:
- Select multiple photos from library
- Arrange in grid or freeform layout
- Add captions to each image
- Export as single composite image

### Offline Capability

**Complete Tasks Offline**:
- Download task instructions and questions while online
- Complete surveys and write text responses offline
- Capture photos/videos offline
- Queue everything for background upload

**Offline Indicator**:
- Banner showing offline status
- List of pending uploads with progress
- Estimate of upload time when back online

## Performance Optimization

### Image Optimization
- Compress images to 80% JPEG quality
- Resize to max 1920x1080 before upload
- Generate thumbnails (320x240) for preview

### Video Optimization
- Target bitrate: 2 Mbps for 720p
- H.264 codec for broad compatibility
- Limit resolution to 1080p max
- Compress audio to AAC 128 kbps

### App Size Optimization
- Code splitting for Android (dynamic feature modules)
- Lazy load screens not needed at launch
- Optimize image assets with WebP format
- Remove unused dependencies

### Battery Optimization
- Limit background sync frequency (every 15 minutes max)
- Use low-power location services
- Throttle network requests
- Release camera resources when not in use

## Security

### Data Encryption
- Encrypt local database with SQLCipher
- Store auth tokens in iOS Keychain / Android Keystore
- Encrypt media files on device storage
- Use HTTPS for all network communication

### Biometric Authentication
- Face ID / Touch ID for iOS
- Fingerprint / Face unlock for Android
- Optional - enabled in settings
- Fallback to PIN/password

### Certificate Pinning
- Pin SSL certificate for API domain
- Prevent man-in-the-middle attacks
- Update pins with app updates

## Testing Strategy

### Unit Tests
- Redux reducers and actions
- API client functions
- Data transformation utilities
- Offline sync logic

### Integration Tests
- Camera capture flow
- Upload with retry logic
- Offline/online transitions
- Push notification handling

### E2E Tests (Detox or Appium)
- Complete survey flow
- Capture and submit video diary
- Upload photo with annotation
- Offline task completion

### Manual Testing
- Test on physical devices (not just emulators)
- Various network conditions (3G, 4G, 5G, Wi-Fi)
- Low battery scenarios
- Low storage scenarios
- Different iOS and Android versions

## Deployment

### App Store Submission

**iOS (App Store)**:
- App Store Connect setup
- Provisioning profiles and certificates
- Screenshots for all device sizes
- Privacy policy link (required)
- App Store review process (typically 1-3 days)

**Android (Google Play)**:
- Google Play Console setup
- Signed APK/AAB with upload key
- Screenshots and feature graphic
- Privacy policy link (required)
- Staged rollout (5% → 20% → 50% → 100%)

### Over-The-Air (OTA) Updates

**CodePush** (Microsoft AppCenter):
- Hot-fix JavaScript changes without app store review
- Staged rollout to percentage of users
- Rollback capability if issues detected
- Does not work for native code changes

### Versioning

**Semantic Versioning**: `MAJOR.MINOR.PATCH`
- MAJOR: Breaking changes (e.g., 2.0.0)
- MINOR: New features, backward compatible (e.g., 1.1.0)
- PATCH: Bug fixes (e.g., 1.0.1)

**Build Numbers**: Increment for every build
- iOS: CFBundleVersion (e.g., 47)
- Android: versionCode (e.g., 47)

## Monitoring and Analytics

### Crash Reporting
- Firebase Crashlytics for crash reports
- Symbolicate crash logs for debugging
- Alert team on critical crashes affecting > 1% of users

### Analytics
- Firebase Analytics for user behavior
- Track feature usage (diary capture, survey completion)
- Measure task completion rates
- Monitor upload success/failure rates

### Performance Monitoring
- Firebase Performance Monitoring
- Track app startup time
- Measure screen load times
- Monitor network request latency

## Platform-Specific Considerations

### iOS Specific
- Implement universal links for deep linking
- Support Dark Mode
- Use native look-and-feel (iOS Human Interface Guidelines)
- Handle iPhone notch and safe areas
- Support iPad with larger layouts

### Android Specific
- Implement App Links for deep linking
- Support multiple screen sizes and densities
- Follow Material Design guidelines
- Handle Android back button correctly
- Support Android 12+ splash screen API

## Future Enhancements

- **Wearable Integration**: Apple Watch / Wear OS for quick task notifications
- **AR Features**: Augmented reality for product placement studies
- **Voice Recording**: Audio-only diaries for hands-free capture
- **Live Streaming**: Real-time video streaming to moderators
- **Peer Collaboration**: Multi-participant mobile collages
- **Gamification**: Badges and achievements for task completion

