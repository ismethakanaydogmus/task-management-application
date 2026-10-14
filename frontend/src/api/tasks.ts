import type { CreateTaskRequest, TaskResponse, UpdateTaskRequest } from '../types/task';
import { apiFetch } from './client';

// Backend'deki TasksController endpoint'lerinin birebir karşılıkları.
// signal: TanStack Query, component ekrandan kalkınca veya sorgu iptal edilince bu sinyalle isteği iptal eder.
// Backend tarafında da CancellationToken tetiklenir (Aşama 3) → zincir uçtan uca iptal edilebilir.

export function getTasks(signal?: AbortSignal) {
  return apiFetch<TaskResponse[]>('/api/tasks', { signal });
}

export function createTask(request: CreateTaskRequest) {
  return apiFetch<TaskResponse>('/api/tasks', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateTask(id: number, request: UpdateTaskRequest) {
  return apiFetch<void>(`/api/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function deleteTask(id: number) {
  return apiFetch<void>(`/api/tasks/${id}`, { method: 'DELETE' });
}
