import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatMessageRequest, ChatMessageResponse } from '../models/chat';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = `${environment.apiUrl}/chat`;
  private sessionIdKey = 'apex_chat_session_id';

  constructor(private http: HttpClient) {}

  public getSessionId(): string {
    let sessionId = sessionStorage.getItem(this.sessionIdKey);
    if (!sessionId) {
      sessionId = 'sess_' + Math.random().toString(36).substring(2, 11) + Date.now();
      sessionStorage.setItem(this.sessionIdKey, sessionId);
    }
    return sessionId;
  }

  public sendMessage(message: string, userId?: number | null, userName?: string | null): Observable<ChatMessageResponse> {
    const payload: ChatMessageRequest = {
      message: message.trim(),
      userId: userId ?? null,
      userName: userName ?? null,
      sessionId: this.getSessionId()
    };

    return this.http.post<ChatMessageResponse>(this.apiUrl, payload).pipe(
      catchError(error => {
        console.warn('ChatService network/API error, providing client fallback:', error);
        const fallback: ChatMessageResponse = {
          reply: "I am having temporary trouble reaching the assistant server. Please check your internet or reach our human team directly at support@apexstore.com.",
          intent: 'Fallback',
          confidence: 0.2,
          suggestedActions: [
            'Explore Products',
            'Shipping Information',
            'Return Policy',
            'Contact Support'
          ],
          timestamp: new Date().toISOString()
        };
        return of(fallback);
      })
    );
  }
}
