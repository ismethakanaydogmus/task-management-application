import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth';
import { apiFetch } from './client';

// { auth: false } → cookie gönderilir/alınır, Bearer eklenmez, 401'de yenileme denenmez.

export function login(request: LoginRequest) {
  return apiFetch<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(request) }, { auth: false });
}

export function register(request: RegisterRequest) {
  return apiFetch<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(request) }, { auth: false });
}

export function logout() {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' }, { auth: false });
}
