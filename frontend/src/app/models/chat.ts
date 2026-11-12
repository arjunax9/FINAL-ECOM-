export interface ChatMessageRequest {
  message: string;
  userId?: number | null;
  userName?: string | null;
  sessionId?: string;
}

export interface ChatProductCard {
  id: number;
  name: string;
  price: number;
  category: string;
  description?: string;
  quantity?: number;
  stock?: number;
  inStock?: boolean;
  imageUrl?: string;
}

export interface ChatOrderCard {
  id?: number;
  orderId?: number;
  createdAt?: string;
  orderDate?: string;
  totalAmount: number;
  status: string;
  itemCount: number;
  itemsSummary?: string[];
}

export interface ChatMessageResponse {
  reply: string;
  intent: string;
  confidence?: number;
  suggestions?: string[];
  suggestedActions?: string[];
  products?: ChatProductCard[];
  order?: ChatOrderCard | null;
  orders?: ChatOrderCard[];
  timestamp: string;
}

export interface UiChatMessage {
  id: string;
  sender: 'user' | 'bot';
  text: string;
  timestamp: Date;
  intent?: string;
  suggestedActions?: string[];
  products?: ChatProductCard[];
  order?: ChatOrderCard | null;
  orders?: ChatOrderCard[];
  isError?: boolean;
}
