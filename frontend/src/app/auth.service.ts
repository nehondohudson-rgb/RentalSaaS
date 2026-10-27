import { API_BASE_URL } from './api.config';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface AuthResponse {
  userId: number;
  email: string;
  role: string;
  accessToken: string;
  expiresAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${API_BASE_URL}/Auth`;
  private readonly tokenKey = 'rentalsaas_access_token';
  private readonly userKey = 'rentalsaas_user';

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, {
      email: email.trim(),
      password
    }).pipe(
      tap(response => {
        sessionStorage.setItem(this.tokenKey, response.accessToken);
        sessionStorage.setItem(this.userKey, JSON.stringify({
          email: response.email,
          role: response.role,
          expiresAtUtc: response.expiresAtUtc
        }));
      })
    );
  }

  getToken(): string | null {
    return sessionStorage.getItem(this.tokenKey);
  }

  getUser(): { email: string; role: string; expiresAtUtc: string } | null {
    const value = sessionStorage.getItem(this.userKey);
    if (!value) return null;

    try {
      const user = JSON.parse(value);
      if (new Date(user.expiresAtUtc).getTime() <= Date.now()) {
        this.logout();
        return null;
      }
      return user;
    } catch {
      this.logout();
      return null;
    }
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null && this.getUser() !== null;
  }

  logout(): void {
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.userKey);
  }
}
