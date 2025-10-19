import { ChatRoom } from './chatService';

const API_BASE = '/api/chat';

export const chatApiService = {
  async getPublicRooms(token: string): Promise<ChatRoom[]> {
    const response = await fetch(`${API_BASE}/rooms`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch chat rooms');
    }

    return response.json();
  },

  async getMyRooms(token: string): Promise<ChatRoom[]> {
    const response = await fetch(`${API_BASE}/my-rooms`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch my chat rooms');
    }

    return response.json();
  },

  async getRoom(token: string, roomId: string): Promise<ChatRoom> {
    const response = await fetch(`${API_BASE}/rooms/${roomId}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch chat room');
    }

    return response.json();
  },

  async createRoom(token: string, name: string, description?: string, isPublic: boolean = true): Promise<ChatRoom> {
    const response = await fetch(`${API_BASE}/rooms`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ name, description, isPublic })
    });

    if (!response.ok) {
      throw new Error('Failed to create chat room');
    }

    return response.json();
  },

  async joinRoom(token: string, roomId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/rooms/${roomId}/join`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to join chat room');
    }
  },

  async leaveRoom(token: string, roomId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/rooms/${roomId}/leave`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to leave chat room');
    }
  },

  async getMessages(token: string, roomId: string, skip: number = 0, take: number = 50): Promise<any[]> {
    const response = await fetch(`${API_BASE}/rooms/${roomId}/messages?skip=${skip}&take=${take}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch messages');
    }

    return response.json();
  },

  async getUnreadCount(token: string, roomId: string): Promise<number> {
    const response = await fetch(`${API_BASE}/rooms/${roomId}/unread-count`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch unread count');
    }

    const data = await response.json();
    return data.unreadCount;
  }
};

