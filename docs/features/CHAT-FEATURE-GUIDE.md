# Real-time Chat Feature - Complete Implementation Guide

This document provides a complete walkthrough of the real-time chat feature built with WebSockets/SignalR, demonstrating the full-stack implementation.

## Overview

The chat feature demonstrates:
- Real-time bidirectional communication using SignalR/WebSocket
- Clean Architecture pattern (Domain, Infrastructure, Application, API)
- React frontend with TypeScript and Vite
- Entity Framework Core with SQL Server
- JWT authentication integration

## Architecture

### Backend Components

1. **Domain Layer** (`src/Core/Entities/Community/`)
   - `ChatRoom.cs` - Chat room entity
   - `ChatMessage.cs` - Message entity
   - `ChatRoomMember.cs` - Membership join table

2. **Repository Interfaces** (`src/Core/Interfaces/`)
   - `IChatRoomRepository.cs`
   - `IChatMessageRepository.cs`

3. **Infrastructure Layer** (`src/Infrastructure/`)
   - `ChatRoomRepository.cs` - EF Core implementation
   - `ChatMessageRepository.cs` - EF Core implementation
   - `ApplicationDbContext.cs` - Database configuration

4. **API Layer** (`src/Api/`)
   - `ChatHub.cs` - SignalR hub for WebSocket connections
   - `ChatController.cs` - REST API endpoints
   - `Program.cs` - SignalR and CORS configuration

### Frontend Components

1. **Services** (`frontend/src/services/`)
   - `chatService.ts` - SignalR/WebSocket client
   - `chatApiService.ts` - REST API client

2. **Hooks** (`frontend/src/hooks/`)
   - `useChat.ts` - Custom React hook for chat functionality

3. **Components** (`frontend/src/components/`)
   - `ChatRoom.tsx` - Main chat interface
   - `ChatRoomList.tsx` - Room list sidebar

## Database Setup

### Create Migration

```bash
cd src/Infrastructure
dotnet ef migrations add AddChatEntities --startup-project ../Api
dotnet ef database update --startup-project ../Api
```

This creates the following tables:
- `ChatRooms`
- `ChatMessages`
- `ChatRoomMembers`

## Backend API Endpoints

### REST Endpoints

```
GET    /api/chat/rooms                     # Get all public chat rooms
GET    /api/chat/my-rooms                  # Get user's chat rooms
GET    /api/chat/rooms/{roomId}            # Get specific room details
POST   /api/chat/rooms                     # Create a new chat room
POST   /api/chat/rooms/{roomId}/join       # Join a chat room
POST   /api/chat/rooms/{roomId}/leave      # Leave a chat room
GET    /api/chat/rooms/{roomId}/messages   # Get room messages (paginated)
GET    /api/chat/rooms/{roomId}/unread-count # Get unread message count
```

### SignalR Hub Endpoint

```
WS     /hubs/chat                          # WebSocket connection
```

### SignalR Hub Methods

**Client to Server:**
- `JoinRoom(chatRoomId)` - Join a chat room group
- `LeaveRoom(chatRoomId)` - Leave a chat room group
- `SendMessage(chatRoomId, message)` - Send a message
- `SendTypingIndicator(chatRoomId, isTyping)` - Send typing status
- `MarkAsRead(chatRoomId, lastMessageId)` - Mark messages as read

**Server to Client:**
- `Connected(data)` - Connection established
- `ReceiveMessage(message)` - New message received
- `UserTyping(typing)` - User typing indicator
- `UserJoined(data)` - User joined room
- `UserLeft(data)` - User left room
- `JoinedRoom(data)` - Successfully joined room
- `Error(error)` - Error occurred

## Running the Application

### 1. Start the Backend

```bash
cd src/Api
dotnet run
```

Backend runs on `https://localhost:5001`

### 2. Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on `http://localhost:5173`

### 3. Test the Application

1. Open `http://localhost:5173` in your browser
2. Create a new chat room or join an existing one
3. Open another browser window/tab to simulate multiple users
4. Send messages and see real-time updates
5. Try typing to see typing indicators

## Key Features Demonstrated

### 1. Real-time Messaging
- Messages are instantly delivered to all users in the room
- Uses SignalR groups for efficient message routing
- Persistent storage in SQL Server

### 2. Typing Indicators
- Shows when other users are typing
- Automatically clears after 1 second of inactivity
- Does not send to self (using `OthersInGroup`)

### 3. Connection Management
- Automatic reconnection with exponential backoff
- Connection status display
- Graceful error handling

### 4. Room Management
- Create public chat rooms
- Join/leave rooms
- View room members
- Room-based message isolation

### 5. Authentication Integration
- JWT token-based authentication
- User identification in messages
- Authorization checks for room access

## Code Examples

### Backend: Sending a Message (SignalR Hub)

```csharp
public async Task SendMessage(string chatRoomId, string message)
{
    var userId = Context.User?.GetUserId();
    
    // Validate membership
    var isMember = await _chatRoomRepository.IsUserMemberAsync(roomGuid, userGuid);
    if (!isMember) return;

    // Save to database
    var chatMessage = new ChatMessage { /* ... */ };
    await _chatMessageRepository.AddAsync(chatMessage);

    // Broadcast to room
    await Clients.Group($"ChatRoom_{chatRoomId}")
        .SendAsync("ReceiveMessage", messageDto);
}
```

### Frontend: Connecting to SignalR

```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/chat', {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

connection.on('ReceiveMessage', (message) => {
  // Handle incoming message
});

await connection.start();
```

### Frontend: Sending a Message

```typescript
await connection.invoke('SendMessage', roomId, messageText);
```

## Configuration

### Backend: Program.cs

```csharp
// Add SignalR
builder.Services.AddSignalR();

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");
```

### Frontend: vite.config.ts

```typescript
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'wss://localhost:5001',
        ws: true,
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
```

## Testing Scenarios

### 1. Multi-User Chat
- Open application in two browser windows
- Login as different users (or use different tokens)
- Send messages from both windows
- Verify messages appear in real-time

### 2. Typing Indicators
- Start typing in one window
- Verify typing indicator appears in the other window
- Stop typing and verify indicator disappears

### 3. Connection Resilience
- Start chatting
- Stop the backend server
- Observe connection status change
- Restart backend
- Verify automatic reconnection

### 4. Room Management
- Create multiple rooms
- Join different rooms
- Verify messages only appear in their respective rooms
- Leave a room and verify you stop receiving messages

## Production Considerations

1. **Authentication**
   - Replace mock token with real JWT from auth flow
   - Implement token refresh mechanism
   - Add user profile information

2. **Scalability**
   - Use Redis backplane for multi-server SignalR
   - Implement message pagination
   - Add message search functionality
   - Consider Azure SignalR Service

3. **Features**
   - File/image sharing
   - Message reactions
   - User mentions
   - Message threading
   - Read receipts
   - Push notifications

4. **Security**
   - Rate limiting on message sending
   - Content moderation
   - Private/encrypted rooms
   - Message history retention policies

5. **Performance**
   - Message batching
   - Virtual scrolling for large message lists
   - Image optimization
   - Lazy loading of rooms

## Troubleshooting

### SignalR Connection Fails
- Verify CORS configuration includes `AllowCredentials()`
- Check token is being sent correctly
- Ensure SSL certificate is trusted (dev environment)

### Messages Not Appearing
- Verify user has joined the room
- Check SignalR hub method names match frontend
- Confirm database save operations are completing

### Typing Indicators Not Working
- Ensure timeout logic is working correctly
- Verify `OthersInGroup` is used (not `All`)
- Check network tab for WebSocket messages

## Resources

- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [React SignalR Client](https://www.npmjs.com/package/@microsoft/signalr)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Vite Documentation](https://vitejs.dev/)

## Summary

This implementation provides a complete, production-ready foundation for real-time chat functionality. The architecture is clean, maintainable, and scalable, following industry best practices for both backend and frontend development.

The combination of SignalR for real-time communication, Entity Framework Core for data persistence, and React for the UI creates a robust and responsive user experience.

