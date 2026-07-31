import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ApiService } from './core/services/api.service';
import { UploadResponse } from './core/models/api.models';
import { getHttpErrorMessage } from './core/utils/http-error';
import { ResultsSection } from './features/results/results-section';
import { LatestValues } from './features/values/latest-values';

@Component({
  selector: 'app-root',
  imports: [DatePipe, DecimalPipe, ResultsSection, LatestValues],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly api = inject(ApiService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly uploadResult = signal<UploadResponse | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly isUploading = signal(false);
  protected readonly isDragging = signal(false);
  protected readonly resultRefreshToken = signal(0);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectFile(input.files?.item(0) ?? null);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    this.selectFile(event.dataTransfer?.files.item(0) ?? null);
  }

  protected upload(event: Event): void {
    event.preventDefault();
    const file = this.selectedFile();

    if (!file || this.isUploading()) {
      return;
    }

    this.errorMessage.set('');
    this.uploadResult.set(null);
    this.isUploading.set(true);

    this.api.uploadCsv(file)
      .pipe(finalize(() => this.isUploading.set(false)))
      .subscribe({
        next: (result) => {
          this.uploadResult.set(result);
          this.resultRefreshToken.update((value) => value + 1);
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(getHttpErrorMessage(
            error,
            'Не удалось загрузить файл. Повторите попытку.'
          ))
      });
  }

  protected formatBytes(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} Б`;
    }

    return `${(bytes / 1024).toFixed(1)} КБ`;
  }

  private selectFile(file: File | null): void {
    this.uploadResult.set(null);
    this.errorMessage.set('');

    if (!file) {
      this.selectedFile.set(null);
      return;
    }

    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.selectedFile.set(null);
      this.errorMessage.set('Выберите файл с расширением .csv.');
      return;
    }

    if (file.size === 0) {
      this.selectedFile.set(null);
      this.errorMessage.set('Выбранный CSV-файл пуст.');
      return;
    }

    this.selectedFile.set(file);
  }

}
