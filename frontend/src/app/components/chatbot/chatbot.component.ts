import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { Router } from '@angular/router';
import { ChatService } from '../../services/chat.service';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { ProductImageService } from '../../services/product-image.service';
import { UiChatMessage, ChatProductCard, ChatOrderCard } from '../../models/chat';
import { Product } from '../../models/product';

@Component({
  selector: 'app-chatbot',
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.css']
})
export class ChatbotComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  isOpen = false;
  isLoading = false;
  inputText = '';
  hasUnread = false;
  unreadCount = 0;
  addedProductIds: { [id: number]: boolean } = {};

  messages: UiChatMessage[] = [];

  defaultSuggestions: string[] = [
    'Track my order',
    'Recommend headphones',
    'Shipping rates & time',
    'Return & refund policy',
    'Payment methods',
    'Contact customer support'
  ];

  constructor(
    private chatService: ChatService,
    private authService: AuthService,
    private cartService: CartService,
    public imgService: ProductImageService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initWelcomeMessage();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  toggleChat(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.hasUnread = false;
      this.unreadCount = 0;
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  openChat(): void {
    this.isOpen = true;
    this.hasUnread = false;
    this.unreadCount = 0;
    setTimeout(() => this.scrollToBottom(), 100);
  }

  closeChat(): void {
    this.isOpen = false;
  }

  restartChat(): void {
    this.messages = [];
    this.initWelcomeMessage();
  }

  private initWelcomeMessage(): void {
    const userName = this.authService.getUserName();
    const greeting = userName ? `Hi ${userName}! 👋` : 'Hello! 👋';

    this.messages.push({
      id: 'welcome_' + Date.now(),
      sender: 'bot',
      text: `${greeting} I'm your Apex AI Concierge. I can help you track orders, discover products, check stock, or answer policy questions. What can I do for you today?`,
      timestamp: new Date(),
      suggestedActions: this.defaultSuggestions
    });
  }

  sendMessage(textToSend?: string): void {
    const rawMessage = (textToSend !== undefined ? textToSend : this.inputText).trim();
    if (!rawMessage || this.isLoading) return;

    const userMsg: UiChatMessage = {
      id: 'user_' + Date.now(),
      sender: 'user',
      text: rawMessage,
      timestamp: new Date()
    };
    this.messages.push(userMsg);
    this.inputText = '';
    this.isLoading = true;

    const userId = this.authService.getUserId();
    const userName = this.authService.getUserName();

    this.chatService.sendMessage(rawMessage, userId, userName).subscribe({
      next: res => {
        this.isLoading = false;
        const suggestions = (res.suggestions && res.suggestions.length > 0)
          ? res.suggestions
          : ((res.suggestedActions && res.suggestedActions.length > 0) ? res.suggestedActions : this.defaultSuggestions);

        const orders: ChatOrderCard[] = [];
        if (res.orders && res.orders.length > 0) {
          orders.push(...res.orders);
        } else if (res.order) {
          orders.push(res.order);
        }

        const botMsg: UiChatMessage = {
          id: 'bot_' + Date.now(),
          sender: 'bot',
          text: res.reply,
          timestamp: new Date(res.timestamp || Date.now()),
          intent: res.intent,
          suggestedActions: suggestions,
          products: res.products,
          orders: orders.length > 0 ? orders : undefined
        };
        this.messages.push(botMsg);

        if (!this.isOpen) {
          this.hasUnread = true;
          this.unreadCount++;
        }
      },
      error: () => {
        this.isLoading = false;
        this.messages.push({
          id: 'err_' + Date.now(),
          sender: 'bot',
          text: "I couldn't reach the support server. Please try again in a moment or email support@apexstore.com.",
          timestamp: new Date(),
          isError: true,
          suggestedActions: ['Try again', 'Explore Products', 'Contact Support']
        });
      }
    });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  handleSuggestion(suggestion: string): void {
    this.sendMessage(suggestion);
  }

  addToCart(productCard: ChatProductCard): void {
    const product: Product = {
      id: productCard.id,
      name: productCard.name,
      description: productCard.description || '',
      price: productCard.price,
      category: productCard.category,
      imageUrl: productCard.imageUrl
    };

    this.cartService.add(product, 1);
    this.addedProductIds[productCard.id] = true;
    setTimeout(() => {
      this.addedProductIds[productCard.id] = false;
    }, 2000);
  }

  viewProduct(id: number): void {
    this.router.navigate(['/products', id]);
  }

  viewOrder(orderId: number): void {
    this.router.navigate(['/orders', orderId]);
  }

  goToOrders(): void {
    this.router.navigate(['/orders']);
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s.includes('deliver') || s.includes('complet')) return 'status-delivered';
    if (s.includes('ship')) return 'status-shipped';
    if (s.includes('process')) return 'status-processing';
    if (s.includes('cancel')) return 'status-cancelled';
    return 'status-pending';
  }

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      try {
        const el = this.messagesContainer.nativeElement;
        el.scrollTop = el.scrollHeight;
      } catch {}
    }
  }
}
