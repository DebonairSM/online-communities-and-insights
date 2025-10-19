# Online Communities - Real-time Chat Frontend

This is the React + TypeScript + Vite frontend for the Online Communities real-time chat application.

## Features

- Real-time messaging using SignalR/WebSocket
- Typing indicators
- Multiple chat rooms
- Create and join public chat rooms
- Modern UI with React and TypeScript

## Prerequisites

- Node.js 18+ and npm
- Backend API running on `https://localhost:5001`

## Installation

```bash
npm install
```

## Running the Application

```bash
npm run dev
```

The application will be available at `http://localhost:5173`

## Building for Production

```bash
npm run build
```

## Project Structure

```
src/
├── components/         # React components
│   ├── ChatRoom.tsx           # Main chat room component
│   └── ChatRoomList.tsx       # Chat room list sidebar
├── hooks/             # Custom React hooks
│   └── useChat.ts             # Hook for chat functionality
├── services/          # API and SignalR services
│   ├── chatService.ts         # SignalR WebSocket service
│   └── chatApiService.ts      # REST API service
├── App.tsx            # Main application component
├── main.tsx           # Application entry point
└── index.css          # Global styles
```

## Technology Stack

- React 18
- TypeScript
- Vite
- @microsoft/signalr (WebSocket client)
- CSS-in-JS for styling

## Notes

- The demo uses a mock token. In production, integrate with your authentication flow.
- Make sure the backend API is running before starting the frontend.
- The Vite dev server proxies API and WebSocket requests to the backend.

