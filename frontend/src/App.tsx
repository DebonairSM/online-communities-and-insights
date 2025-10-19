import React, { useState } from 'react';
import { ChatRoom } from './components/ChatRoom';
import { ChatRoomList } from './components/ChatRoomList';

function App() {
  // For demo purposes, using a mock token. In production, get this from your auth flow
  const [token] = useState<string>('demo-token-replace-with-real-jwt');
  const [selectedRoomId, setSelectedRoomId] = useState<string | null>(null);
  const [selectedRoomName, setSelectedRoomName] = useState<string>('');

  const handleSelectRoom = (roomId: string, roomName: string) => {
    setSelectedRoomId(roomId);
    setSelectedRoomName(roomName);
  };

  return (
    <div style={styles.app}>
      <header style={styles.header}>
        <h1 style={styles.appTitle}>Online Communities - Real-time Chat Demo</h1>
        <p style={styles.subtitle}>
          Built with .NET 9, SignalR, React, and Vite
        </p>
      </header>

      <div style={styles.content}>
        <div style={styles.sidebar}>
          <ChatRoomList 
            token={token}
            onSelectRoom={handleSelectRoom}
            selectedRoomId={selectedRoomId}
          />
        </div>

        <div style={styles.main}>
          {selectedRoomId ? (
            <ChatRoom 
              token={token}
              roomId={selectedRoomId}
              roomName={selectedRoomName}
            />
          ) : (
            <div style={styles.placeholder}>
              <h2 style={styles.placeholderTitle}>Welcome to Real-time Chat</h2>
              <p style={styles.placeholderText}>
                Select a chat room from the list or create a new one to start chatting
              </p>
              <div style={styles.features}>
                <div style={styles.feature}>
                  <div style={styles.featureIcon}>⚡</div>
                  <h3 style={styles.featureTitle}>Real-time Messaging</h3>
                  <p style={styles.featureDescription}>
                    Messages are instantly delivered via WebSocket/SignalR
                  </p>
                </div>
                <div style={styles.feature}>
                  <div style={styles.featureIcon}>👥</div>
                  <h3 style={styles.featureTitle}>Multi-user Support</h3>
                  <p style={styles.featureDescription}>
                    Multiple users can chat in the same room simultaneously
                  </p>
                </div>
                <div style={styles.feature}>
                  <div style={styles.featureIcon}>✏️</div>
                  <h3 style={styles.featureTitle}>Typing Indicators</h3>
                  <p style={styles.featureDescription}>
                    See when other users are typing a message
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      <footer style={styles.footer}>
        <p style={styles.footerText}>
          Technology Stack: ASP.NET Core 9 + Entity Framework Core + SignalR + React 18 + TypeScript + Vite
        </p>
      </footer>
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  app: {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: '#f3f4f6'
  },
  header: {
    padding: '24px',
    backgroundColor: '#1f2937',
    color: '#ffffff',
    textAlign: 'center'
  },
  appTitle: {
    margin: '0 0 8px 0',
    fontSize: '32px',
    fontWeight: '700'
  },
  subtitle: {
    margin: 0,
    fontSize: '16px',
    color: '#9ca3af'
  },
  content: {
    flex: 1,
    display: 'flex',
    gap: '20px',
    padding: '20px',
    maxWidth: '1400px',
    margin: '0 auto',
    width: '100%'
  },
  sidebar: {
    flexShrink: 0
  },
  main: {
    flex: 1,
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center'
  },
  placeholder: {
    maxWidth: '600px',
    padding: '40px',
    textAlign: 'center'
  },
  placeholderTitle: {
    margin: '0 0 16px 0',
    fontSize: '28px',
    fontWeight: '600',
    color: '#111827'
  },
  placeholderText: {
    margin: '0 0 40px 0',
    fontSize: '16px',
    color: '#6b7280',
    lineHeight: '1.6'
  },
  features: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: '20px',
    marginTop: '40px'
  },
  feature: {
    padding: '20px',
    backgroundColor: '#ffffff',
    borderRadius: '8px',
    border: '1px solid #e5e7eb'
  },
  featureIcon: {
    fontSize: '32px',
    marginBottom: '12px'
  },
  featureTitle: {
    margin: '0 0 8px 0',
    fontSize: '16px',
    fontWeight: '600',
    color: '#111827'
  },
  featureDescription: {
    margin: 0,
    fontSize: '14px',
    color: '#6b7280',
    lineHeight: '1.5'
  },
  footer: {
    padding: '16px',
    backgroundColor: '#ffffff',
    borderTop: '1px solid #e5e7eb',
    textAlign: 'center'
  },
  footerText: {
    margin: 0,
    fontSize: '14px',
    color: '#6b7280'
  }
};

export default App;

