import type {
  CreateTaskRequest,
  PagedResult,
  TaskQuery,
  TaskResponse,
  TaskSummary,
  UpdateTaskRequest,
} from '../types/task';
import { apiFetch } from './client';

// Backend'deki TasksController endpoint'lerinin birebir karşılıkları.
// signal: TanStack Query, sorgu iptal edilince (ör. filtre hızla değişti) bu sinyalle isteği iptal eder.

export function getTasks(query: TaskQuery, signal?: AbortSignal) {
  // URLSearchParams: değerleri URL'ye güvenle kodlar ("%50 indirim" → "%2550+indirim").
  // Elle `?search=${search}` yazsaydık "&" veya "%" içeren aramalar URL'yi bozardı.
  const params = new URLSearchParams({
    status: query.status,
    sortBy: query.sortBy,
    sortDirection: query.sortDirection,
    page: String(query.page),
    pageSize: String(query.pageSize),
  });
  if (query.priority) params.set('priority', query.priority);
  if (query.search.trim()) params.set('search', query.search.trim());

  return apiFetch<PagedResult<TaskResponse>>(`/api/tasks?${params}`, { signal });
}

export function getTaskSummary(signal?: AbortSignal) {
  return apiFetch<TaskSummary>('/api/tasks/summary', { signal });
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
