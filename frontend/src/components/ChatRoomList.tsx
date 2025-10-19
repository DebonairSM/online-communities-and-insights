import React, { useState, useEffect } from 'react';
import { chatApiService } from '../services/chatApiService';
import { ChatRoom } from '../services/chatService';

interface ChatRoomListProps {
  token: string;
  onSelectRoom: (roomId: string, roomName: string) => void;
  selectedRoomId: string | null;
}

export const ChatRoomList: React.FC<ChatRoomListProps> = ({ 
  token, 
  onSelectRoom,
  selectedRoomId 
}) => {
  const [rooms, setRooms] = useState<ChatRoom[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newRoomName, setNewRoomName] = useState('');
  const [newRoomDescription, setNewRoomDescription] = useState('');

  useEffect(() => {
    loadRooms();
  }, [token]);

  const loadRooms = async () => {
    try {
      setLoading(true);
      const publicRooms = await chatApiService.getPublicRooms(token);
      setRooms(publicRooms);
      setError(null);
    } catch (err) {
      setError('Failed to load chat rooms');
      console.error('Load rooms error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRoom = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!newRoomName.trim()) {
      return;
    }

    try {
      const newRoom = await chatApiService.createRoom(
        token, 
        newRoomName.trim(), 
        newRoomDescription.trim() || undefined,
        true
      );
      
      setRooms(prev => [newRoom, ...prev]);
      setNewRoomName('');
      setNewRoomDescription('');
      setShowCreateForm(false);
      setError(null);
      
      // Auto-select the newly created room
      onSelectRoom(newRoom.id, newRoom.name);
    } catch (err) {
      setError('Failed to create room');
      console.error('Create room error:', err);
    }
  };

  const handleJoinRoom = async (room: ChatRoom) => {
    if (!room.isMember) {
      try {
        await chatApiService.joinRoom(token, room.id);
        // Reload rooms to update membership status
        await loadRooms();
      } catch (err) {
        setError('Failed to join room');
        console.error('Join room error:', err);
        return;
      }
    }
    
    onSelectRoom(room.id, room.name);
  };

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading chat rooms...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h2 style={styles.title}>Chat Rooms</h2>
        <button 
          onClick={() => setShowCreateForm(!showCreateForm)}
          style={styles.createButton}
        >
          {showCreateForm ? 'Cancel' : '+ New Room'}
        </button>
      </div>

      {error && (
        <div style={styles.error}>{error}</div>
      )}

      {showCreateForm && (
        <form onSubmit={handleCreateRoom} style={styles.createForm}>
          <input
            type="text"
            placeholder="Room name"
            value={newRoomName}
            onChange={(e) => setNewRoomName(e.target.value)}
            style={styles.input}
            required
          />
          <input
            type="text"
            placeholder="Description (optional)"
            value={newRoomDescription}
            onChange={(e) => setNewRoomDescription(e.target.value)}
            style={styles.input}
          />
          <button type="submit" style={styles.submitButton}>
            Create Room
          </button>
        </form>
      )}

      <div style={styles.roomList}>
        {rooms.length === 0 ? (
          <div style={styles.emptyState}>
            No chat rooms available. Create one to get started!
          </div>
        ) : (
          rooms.map(room => (
            <div
              key={room.id}
              onClick={() => handleJoinRoom(room)}
              style={{
                ...styles.roomItem,
                ...(selectedRoomId === room.id ? styles.roomItemSelected : {})
              }}
            >
              <div style={styles.roomHeader}>
                <h3 style={styles.roomName}>{room.name}</h3>
                {!room.isMember && (
                  <span style={styles.badge}>Join</span>
                )}
              </div>
              {room.description && (
                <p style={styles.roomDescription}>{room.description}</p>
              )}
              <div style={styles.roomMeta}>
                <span style={styles.memberCount}>
                  {room.memberCount} {room.memberCount === 1 ? 'member' : 'members'}
                </span>
                {room.isPublic && (
                  <span style={styles.publicBadge}>Public</span>
                )}
              </div>
            </div>
          ))
        )}
      </div>

      <button onClick={loadRooms} style={styles.refreshButton}>
        Refresh Rooms
      </button>
    </div>
  );
};

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '600px',
    width: '300px',
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
    padding: '16px',
    borderBottom: '1px solid #e5e7eb',
    backgroundColor: '#f9fafb'
  },
  title: {
    margin: 0,
    fontSize: '18px',
    fontWeight: '600',
    color: '#111827'
  },
  createButton: {
    padding: '6px 12px',
    backgroundColor: '#3b82f6',
    color: '#ffffff',
    border: 'none',
    borderRadius: '4px',
    fontSize: '13px',
    fontWeight: '600',
    cursor: 'pointer'
  },
  loading: {
    padding: '40px',
    textAlign: 'center',
    color: '#6b7280'
  },
  error: {
    padding: '12px 16px',
    backgroundColor: '#fee2e2',
    color: '#991b1b',
    fontSize: '14px',
    borderBottom: '1px solid #fecaca'
  },
  createForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    padding: '16px',
    borderBottom: '1px solid #e5e7eb',
    backgroundColor: '#f9fafb'
  },
  input: {
    padding: '8px 12px',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '14px',
    outline: 'none'
  },
  submitButton: {
    padding: '8px 12px',
    backgroundColor: '#10b981',
    color: '#ffffff',
    border: 'none',
    borderRadius: '4px',
    fontSize: '14px',
    fontWeight: '600',
    cursor: 'pointer'
  },
  roomList: {
    flex: 1,
    overflowY: 'auto',
    padding: '8px'
  },
  emptyState: {
    padding: '40px 16px',
    textAlign: 'center',
    color: '#9ca3af',
    fontSize: '14px'
  },
  roomItem: {
    padding: '12px',
    marginBottom: '8px',
    border: '1px solid #e5e7eb',
    borderRadius: '6px',
    cursor: 'pointer',
    transition: 'all 0.2s',
    backgroundColor: '#ffffff'
  },
  roomItemSelected: {
    backgroundColor: '#eff6ff',
    borderColor: '#3b82f6'
  },
  roomHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '4px'
  },
  roomName: {
    margin: 0,
    fontSize: '15px',
    fontWeight: '600',
    color: '#111827'
  },
  badge: {
    padding: '2px 8px',
    backgroundColor: '#dbeafe',
    color: '#1e40af',
    borderRadius: '12px',
    fontSize: '11px',
    fontWeight: '600'
  },
  roomDescription: {
    margin: '4px 0',
    fontSize: '13px',
    color: '#6b7280',
    lineHeight: '1.4'
  },
  roomMeta: {
    display: 'flex',
    gap: '8px',
    marginTop: '8px',
    fontSize: '12px'
  },
  memberCount: {
    color: '#6b7280'
  },
  publicBadge: {
    padding: '2px 6px',
    backgroundColor: '#dcfce7',
    color: '#15803d',
    borderRadius: '10px',
    fontSize: '11px',
    fontWeight: '600'
  },
  refreshButton: {
    padding: '12px',
    backgroundColor: '#f9fafb',
    color: '#374151',
    border: 'none',
    borderTop: '1px solid #e5e7eb',
    fontSize: '14px',
    fontWeight: '500',
    cursor: 'pointer'
  }
};

