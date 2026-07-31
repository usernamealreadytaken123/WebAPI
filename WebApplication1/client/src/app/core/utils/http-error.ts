import { HttpErrorResponse } from '@angular/common/http';

export function getHttpErrorMessage(
  error: HttpErrorResponse,
  fallbackMessage: string
): string {
  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error;
  }

  if (typeof error.error?.title === 'string') {
    return error.error.title;
  }

  if (error.status === 0) {
    return 'Не удалось подключиться к WebAPI. Убедитесь, что сервер запущен.';
  }

  return fallbackMessage;
}
