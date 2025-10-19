import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface ChatMessage {
  id: string;
  chatRoomId: string;
  userId: string;
  userEmail: string;
  content: string;
  messageType: string;
  timestamp: string;
  isEdited: boolean;
}

export interface ChatRoom {
  id: string;
  name: string;
  description?: string;
  isPublic: boolean;
  tenantId?: string;
  memberCount: number;
  isMember: boolean;
  createdAt: string;
}

export interface UserTyping {
  userId: string;
  userEmail: string;
  chatRoomId: string;
  isTyping: boolean;
  timestamp: string;
}

class ChatService {
  private connection: HubConnection | null = null;
  private onMessageCallback?: (message: ChatMessage) => void;
  private onTypingCallback?: (typing: UserTyping) => void;
  private onConnectionCallback?: (connected: boolean) => void;
  private onErrorCallback?: (error: { message: string }) => void;

  async connect(token: string): Promise<void> {
    if (this.connection?.state === 'Connected') {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.previousRetryCount === 0) return 0;
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        }
      })
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();

    try {
      await this.connection.start();
      console.log('SignalR Chat connection established');
      this.onConnectionCallback?.(true);
    } catch (error) {
      console.error('Error establishing SignalR connection:', error);
      this.onConnectionCallback?.(false);
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.onConnectionCallback?.(false);
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('Connected', (data: any) => {
      console.log('Connected to ChatHub:', data);
      this.onConnectionCallback?.(true);
    });

    this.connection.on('ReceiveMessage', (message: ChatMessage) => {
      console.log('Received message:', message);
      this.onMessageCallback?.(message);
    });

    this.connection.on('UserTyping', (typing: UserTyping) => {
      this.onTypingCallback?.(typing);
    });

    this.connection.on('UserJoined', (data: any) => {
      console.log('User joined room:', data);
    });

    this.connection.on('UserLeft', (data: any) => {
      console.log('User left room:', data);
    });

    this.connection.on('JoinedRoom', (data: any) => {
      console.log('Successfully joined room:', data);
    });

    this.connection.on('Error', (error: { message: string }) => {
      console.error('SignalR error:', error);
      this.onErrorCallback?.(error);
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.onConnectionCallback?.(false);
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
      this.onConnectionCallback?.(false);
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.onConnectionCallback?.(true);
    });
  }

  async joinRoom(chatRoomId: string): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('JoinRoom', chatRoomId);
    }
  }

  async leaveRoom(chatRoomId: string): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('LeaveRoom', chatRoomId);
    }
  }

  async sendMessage(chatRoomId: string, message: string): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('SendMessage', chatRoomId, message);
    }
  }

  async sendTypingIndicator(chatRoomId: string, isTyping: boolean): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('SendTypingIndicator', chatRoomId, isTyping);
    }
  }

  async markAsRead(chatRoomId: string, lastMessageId: string): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('MarkAsRead', chatRoomId, lastMessageId);
    }
  }

  setMessageCallback(callback: (message: ChatMessage) => void): void {
    this.onMessageCallback = callback;
  }

  setTypingCallback(callback: (typing: UserTyping) => void): void {
    this.onTypingCallback = callback;
  }

  setConnectionCallback(callback: (connected: boolean) => void): void {
    this.onConnectionCallback = callback;
  }

  setErrorCallback(callback: (error: { message: string }) => void): void {
    this.onErrorCallback = callback;
  }

  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }
}

export const chatService = new ChatService();

