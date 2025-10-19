import { useEffect, useState, useCallback, useRef } from 'react';
import { chatService, ChatMessage, UserTyping } from '../services/chatService';

interface UseChatOptions {
  token: string | null;
  autoConnect?: boolean;
}

export const useChat = ({ token, autoConnect = true }: UseChatOptions) => {
  const [isConnected, setIsConnected] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [typingUsers, setTypingUsers] = useState<Map<string, UserTyping>>(new Map());
  const [error, setError] = useState<string | null>(null);
  const [currentRoom, setCurrentRoom] = useState<string | null>(null);
  const typingTimeoutRef = useRef<Map<string, NodeJS.Timeout>>(new Map());

  const handleMessage = useCallback((message: ChatMessage) => {
    setMessages(prev => [...prev, message]);
    setError(null);
  }, []);

  const handleTyping = useCallback((typing: UserTyping) => {
    if (!typing.isTyping) {
      setTypingUsers(prev => {
        const next = new Map(prev);
        next.delete(typing.userId);
        return next;
      });
      return;
    }

    setTypingUsers(prev => {
      const next = new Map(prev);
      next.set(typing.userId, typing);
      return next;
    });

    // Clear existing timeout for this user
    const existingTimeout = typingTimeoutRef.current.get(typing.userId);
    if (existingTimeout) {
      clearTimeout(existingTimeout);
    }

    // Set new timeout to remove typing indicator after 3 seconds
    const timeout = setTimeout(() => {
      setTypingUsers(prev => {
        const next = new Map(prev);
        next.delete(typing.userId);
        return next;
      });
      typingTimeoutRef.current.delete(typing.userId);
    }, 3000);

    typingTimeoutRef.current.set(typing.userId, timeout);
  }, []);

  const handleConnectionChange = useCallback((connected: boolean) => {
    setIsConnected(connected);
  }, []);

  const handleError = useCallback((error: { message: string }) => {
    setError(error.message);
  }, []);

  useEffect(() => {
    chatService.setMessageCallback(handleMessage);
    chatService.setTypingCallback(handleTyping);
    chatService.setConnectionCallback(handleConnectionChange);
    chatService.setErrorCallback(handleError);

    return () => {
      chatService.setMessageCallback(() => {});
      chatService.setTypingCallback(() => {});
      chatService.setConnectionCallback(() => {});
      chatService.setErrorCallback(() => {});
    };
  }, [handleMessage, handleTyping, handleConnectionChange, handleError]);

  useEffect(() => {
    if (token && autoConnect) {
      chatService.connect(token);
    }

    return () => {
      if (currentRoom) {
        chatService.leaveRoom(currentRoom);
      }
      chatService.disconnect();
      
      // Clear all typing timeouts
      typingTimeoutRef.current.forEach(timeout => clearTimeout(timeout));
      typingTimeoutRef.current.clear();
    };
  }, [token, autoConnect]);

  const joinRoom = useCallback(async (roomId: string) => {
    if (!isConnected) {
      setError('Not connected to chat server');
      return;
    }

    try {
      await chatService.joinRoom(roomId);
      setCurrentRoom(roomId);
      setMessages([]); // Clear messages when joining new room
      setTypingUsers(new Map()); // Clear typing indicators
      setError(null);
    } catch (err) {
      setError('Failed to join room');
      console.error('Join room error:', err);
    }
  }, [isConnected]);

  const leaveRoom = useCallback(async (roomId: string) => {
    try {
      await chatService.leaveRoom(roomId);
      if (currentRoom === roomId) {
        setCurrentRoom(null);
        setMessages([]);
        setTypingUsers(new Map());
      }
      setError(null);
    } catch (err) {
      setError('Failed to leave room');
      console.error('Leave room error:', err);
    }
  }, [currentRoom]);

  const sendMessage = useCallback(async (roomId: string, message: string) => {
    if (!isConnected) {
      setError('Not connected to chat server');
      return;
    }

    try {
      await chatService.sendMessage(roomId, message);
      setError(null);
    } catch (err) {
      setError('Failed to send message');
      console.error('Send message error:', err);
    }
  }, [isConnected]);

  const sendTypingIndicator = useCallback(async (roomId: string, isTyping: boolean) => {
    if (!isConnected) return;

    try {
      await chatService.sendTypingIndicator(roomId, isTyping);
    } catch (err) {
      console.error('Send typing indicator error:', err);
    }
  }, [isConnected]);

  const markAsRead = useCallback(async (roomId: string, lastMessageId: string) => {
    if (!isConnected) return;

    try {
      await chatService.markAsRead(roomId, lastMessageId);
    } catch (err) {
      console.error('Mark as read error:', err);
    }
  }, [isConnected]);

  const clearMessages = useCallback(() => {
    setMessages([]);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    isConnected,
    messages,
    typingUsers: Array.from(typingUsers.values()),
    error,
    currentRoom,
    connectionState: chatService.getConnectionState(),
    joinRoom,
    leaveRoom,
    sendMessage,
    sendTypingIndicator,
    markAsRead,
    clearMessages,
    clearError
  };
};

