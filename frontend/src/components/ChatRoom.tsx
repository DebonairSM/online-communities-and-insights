import React, { useState, useEffect, useRef } from 'react';
import { useChat } from '../hooks/useChat';
import { chatApiService } from '../services/chatApiService';

interface ChatRoomProps {
  token: string;
  roomId: string;
  roomName: string;
}

export const ChatRoom: React.FC<ChatRoomProps> = ({ token, roomId, roomName }) => {
  const [messageInput, setMessageInput] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const {
    isConnected,
    messages,
    typingUsers,
    error,
    joinRoom,
    sendMessage,
    sendTypingIndicator,
    clearError
  } = useChat({ token, autoConnect: true });

  useEffect(() => {
    if (isConnected && roomId) {
      joinRoom(roomId);
    }
  }, [isConnected, roomId, joinRoom]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setMessageInput(value);

    if (!isTyping && value.length > 0) {
      setIsTyping(true);
      sendTypingIndicator(roomId, true);
    }

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      sendTypingIndicator(roomId, false);
    }, 1000);
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!messageInput.trim() || !isConnected) {
      return;
    }

    await sendMessage(roomId, messageInput.trim());
    setMessageInput('');
    setIsTyping(false);
    sendTypingIndicator(roomId, false);

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }
  };

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h2 style={styles.title}>{roomName}</h2>
        <div style={styles.connectionStatus}>
          <span style={{
            ...styles.statusDot,
            backgroundColor: isConnected ? '#10b981' : '#ef4444'
          }} />
          {isConnected ? 'Connected' : 'Disconnected'}
        </div>
      </div>

      {error && (
        <div style={styles.errorBar}>
          <span>{error}</span>
          <button onClick={clearError} style={styles.closeButton}>×</button>
        </div>
      )}

      <div style={styles.messagesContainer}>
        {messages.length === 0 ? (
          <div style={styles.emptyState}>
            No messages yet. Start the conversation!
          </div>
        ) : (
          messages.map((msg, index) => (
            <div key={msg.id || index} style={styles.messageWrapper}>
              <div style={styles.messageHeader}>
                <strong style={styles.userEmail}>{msg.userEmail}</strong>
                <span style={styles.timestamp}>{formatTime(msg.timestamp)}</span>
              </div>
              <div style={styles.messageContent}>{msg.content}</div>
            </div>
          ))
        )}
        
        {typingUsers.length > 0 && (
          <div style={styles.typingIndicator}>
            {typingUsers.map(user => user.userEmail).join(', ')} {typingUsers.length === 1 ? 'is' : 'are'} typing...
          </div>
        )}
        
        <div ref={messagesEndRef} />
      </div>

      <form onSubmit={handleSendMessage} style={styles.inputForm}>
        <input
          type="text"
          value={messageInput}
          onChange={handleInputChange}
          placeholder="Type a message..."
          disabled={!isConnected}
          style={{
            ...styles.input,
            opacity: isConnected ? 1 : 0.5
          }}
        />
        <button 
          type="submit" 
          disabled={!isConnected || !messageInput.trim()}
          style={{
            ...styles.sendButton,
            opacity: (isConnected && messageInput.trim()) ? 1 : 0.5
          }}
        >
          Send
        </button>
      </form>
    </div>
  );
};

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '600px',
    maxWidth: '800px',
    margin: '0 auto',
    border: '1px solid #e5e7eb',
    borderRadius: '8px',
    backgroundColor: '#ffffff',
    overflow: 'hidden',
    boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '16px 20px',
    borderBottom: '1px solid #e5e7eb',
    backgroundColor: '#f9fafb'
  },
  title: {
    margin: 0,
    fontSize: '20px',
    fontWeight: '600',
    color: '#111827'
  },
  connectionStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    fontSize: '14px',
    color: '#6b7280'
  },
  statusDot: {
    width: '8px',
    height: '8px',
    borderRadius: '50%'
  },
  errorBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '12px 20px',
    backgroundColor: '#fee2e2',
    color: '#991b1b',
    fontSize: '14px'
  },
  closeButton: {
    background: 'none',
    border: 'none',
    fontSize: '24px',
    cursor: 'pointer',
    color: '#991b1b',
    padding: '0',
    lineHeight: '1'
  },
  messagesContainer: {
    flex: 1,
    overflowY: 'auto',
    padding: '20px',
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
    backgroundColor: '#f9fafb'
  },
  emptyState: {
    textAlign: 'center',
    color: '#9ca3af',
    padding: '40px',
    fontSize: '14px'
  },
  messageWrapper: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    padding: '12px 16px',
    backgroundColor: '#ffffff',
    borderRadius: '8px',
    border: '1px solid #e5e7eb'
  },
  messageHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '4px'
  },
  userEmail: {
    fontSize: '14px',
    color: '#3b82f6',
    fontWeight: '600'
  },
  timestamp: {
    fontSize: '12px',
    color: '#9ca3af'
  },
  messageContent: {
    fontSize: '14px',
    color: '#111827',
    lineHeight: '1.5',
    wordBreak: 'break-word'
  },
  typingIndicator: {
    fontSize: '13px',
    color: '#6b7280',
    fontStyle: 'italic',
    padding: '8px 0'
  },
  inputForm: {
    display: 'flex',
    gap: '12px',
    padding: '16px 20px',
    borderTop: '1px solid #e5e7eb',
    backgroundColor: '#ffffff'
  },
  input: {
    flex: 1,
    padding: '12px 16px',
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    fontSize: '14px',
    outline: 'none',
    transition: 'border-color 0.2s'
  },
  sendButton: {
    padding: '12px 24px',
    backgroundColor: '#3b82f6',
    color: '#ffffff',
    border: 'none',
    borderRadius: '6px',
    fontSize: '14px',
    fontWeight: '600',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  }
};

