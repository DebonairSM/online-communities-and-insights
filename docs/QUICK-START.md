# Real-time Chat Feature - Quick Start Guide

## What You Have

A complete real-time chat application with:
- .NET 9 backend with SignalR WebSocket support
- React + TypeScript + Vite frontend
- Entity Framework Core for data persistence
- JWT authentication integration

## 5-Minute Quick Start

### Step 1: Create Database Tables

```bash
cd src/Infrastructure
dotnet ef migrations add AddChatEntities --startup-project ../Api
dotnet ef database update --startup-project ../Api
```

### Step 2: Start Backend

```bash
cd src/Api
dotnet run
```

Backend runs at: `https://localhost:5001`

### Step 3: Start Frontend

Open a new terminal:

```bash
cd frontend
npm install
npm run dev
```

Frontend runs at: `http://localhost:5173`

### Step 4: Test the Chat

1. Open `http://localhost:5173` in your browser
2. Click **"+ New Room"** button
3. Enter "General Chat" as the room name
4. Click **"Create Room"**
5. The room will appear in the list and automatically open
6. Type a message and press **Send**

### Step 5: Test Real-time (Multi-User)

1. Open another browser window (or incognito window)
2. Go to `http://localhost:5173`
3. Join the "General Chat" room from the list
4. Send messages from both windows
5. Watch messages appear instantly in both windows!

## What to Try

### Typing Indicators
- Start typing in one window
- Watch "is typing..." appear in the other window
- Stop typing and watch it disappear

### Connection Status
- Look at the top-right corner for connection status (green dot = connected)
- Stop the backend server to see status change to disconnected
- Restart backend and watch automatic reconnection

### Multiple Rooms
- Create multiple chat rooms
- Switch between them
- Messages only appear in their respective rooms

## Troubleshooting

### "Failed to load chat rooms"
- Make sure backend is running on `https://localhost:5001`
- Check browser console for CORS errors
- Verify database migration completed successfully

### "Not connected to chat server"
- Check if backend is running
- Look for SignalR errors in browser console
- Verify CORS is configured with `AllowCredentials()`

### Database errors
- Ensure SQL Server is running
- Check connection string in `appsettings.json`
- Run migrations again if tables are missing

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                       Browser Client                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              React Application                       │  │
│  │  ┌──────────────┐  ┌──────────────┐                 │  │
│  │  │ ChatRoomList │  │   ChatRoom   │                 │  │
│  │  └──────┬───────┘  └──────┬───────┘                 │  │
│  │         │                  │                          │  │
│  │         └──────────┬───────┘                          │  │
│  │                    │                                   │  │
│  │              ┌─────▼─────┐                           │  │
│  │              │  useChat  │  (React Hook)             │  │
│  │              └─────┬─────┘                           │  │
│  │                    │                                   │  │
│  │      ┌─────────────┴─────────────┐                   │  │
│  │      │                           │                    │  │
│  │ ┌────▼─────────┐      ┌─────────▼──────┐           │  │
│  │ │ chatService  │      │ chatApiService │           │  │
│  │ │  (SignalR)   │      │     (REST)     │           │  │
│  │ └────┬─────────┘      └─────────┬──────┘           │  │
│  └──────┼────────────────────────────┼──────────────────┘  │
└─────────┼────────────────────────────┼─────────────────────┘
          │                            │
          │ WebSocket                  │ HTTP
          │ (wss://)                   │ (https://)
          │                            │
┌─────────▼────────────────────────────▼─────────────────────┐
│                    ASP.NET Core API                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                  API Layer                            │  │
│  │  ┌──────────┐                ┌───────────────────┐  │  │
│  │  │ ChatHub  │ ◄─────────────►│ ChatController    │  │  │
│  │  │(SignalR) │    Uses         │ (REST Endpoints)  │  │  │
│  │  └────┬─────┘                 └─────────┬─────────┘  │  │
│  └───────┼──────────────────────────────────┼───────────┘  │
│          │                                   │               │
│  ┌───────▼───────────────────────────────────▼───────────┐ │
│  │              Infrastructure Layer                      │ │
│  │  ┌──────────────────────┐  ┌──────────────────────┐  │ │
│  │  │ ChatRoomRepository   │  │ ChatMessageRepo      │  │ │
│  │  └──────────┬───────────┘  └──────────┬───────────┘  │ │
│  └─────────────┼──────────────────────────┼──────────────┘ │
│                │                           │                 │
│  ┌─────────────▼───────────────────────────▼──────────────┐│
│  │          Entity Framework Core (EF Core)                ││
│  └─────────────────────────────┬──────────────────────────┘│
└────────────────────────────────┼───────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │      SQL Server         │
                    │  ┌──────────────────┐  │
                    │  │   ChatRooms      │  │
                    │  │   ChatMessages   │  │
                    │  │   ChatRoomMembers│  │
                    │  └──────────────────┘  │
                    └─────────────────────────┘
```

## File Structure

```
Backend:
  src/Core/Entities/Community/
    - ChatRoom.cs
    - ChatMessage.cs
    - ChatRoomMember.cs
  
  src/Core/Interfaces/
    - IChatRoomRepository.cs
    - IChatMessageRepository.cs
  
  src/Infrastructure/Repositories/
    - ChatRoomRepository.cs
    - ChatMessageRepository.cs
  
  src/Api/
    - Hubs/ChatHub.cs
    - Controllers/ChatController.cs

Frontend:
  frontend/src/
    - services/
      - chatService.ts (SignalR)
      - chatApiService.ts (REST)
    - hooks/
      - useChat.ts
    - components/
      - ChatRoom.tsx
      - ChatRoomList.tsx
    - App.tsx
```

## Next Steps

1. **Customize Authentication**
   - Replace mock token in `App.tsx` with real JWT
   - Integrate with your auth provider

2. **Styling**
   - Modify inline styles in components
   - Add a UI library like Material-UI or Chakra UI

3. **Features**
   - Add message reactions
   - Implement file sharing
   - Add user avatars
   - Create private messages

4. **Production**
   - Add Redis backplane for SignalR
   - Implement rate limiting
   - Add message search
   - Set up monitoring

## Documentation

- **CHAT-FEATURE-GUIDE.md** - Complete implementation details
- **IMPLEMENTATION-SUMMARY.md** - Overview of what was built
- **frontend/README.md** - Frontend-specific documentation

## Support

If you encounter issues:
1. Check browser console for errors
2. Check backend logs
3. Verify database connection
4. Review the comprehensive guides mentioned above

Enjoy your real-time chat feature! 🚀

