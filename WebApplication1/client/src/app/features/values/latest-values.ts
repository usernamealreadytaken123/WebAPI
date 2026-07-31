import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, Input, OnChanges, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { ValueResponse } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { getHttpErrorMessage } from '../../core/utils/http-error';

@Component({
  selector: 'app-latest-values',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  templateUrl: './latest-values.html',
  styleUrl: './latest-values.scss'
})
export class LatestValues implements OnChanges {
  private readonly api = inject(ApiService);

  @Input() initialFileName = '';
  @Input() refreshToken = 0;

  protected readonly fileNameControl = new FormControl('', { nonNullable: true });
  protected readonly values = signal<ValueResponse[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isSearched = signal(false);
  protected readonly errorMessage = signal('');

  ngOnChanges(): void {
    const fileName = this.initialFileName.trim();

    if (fileName) {
      this.fileNameControl.setValue(fileName);
      this.loadValues();
    }
  }

  protected search(event: Event): void {
    event.preventDefault();
    this.loadValues();
  }

  protected loadValues(): void {
    const fileName = this.fileNameControl.value.trim();

    if (!fileName) {
      this.values.set([]);
      this.isSearched.set(false);
      this.errorMessage.set('Укажите имя CSV-файла.');
      return;
    }

    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    this.isLoading.set(true);

    this.api.getLatestValues(fileName)
      .pipe(finalize(() => {
        this.isLoading.set(false);
        this.isSearched.set(true);
      }))
      .subscribe({
        next: (values) => this.values.set(values),
        error: (error: HttpErrorResponse) => {
          this.values.set([]);
          this.errorMessage.set(getHttpErrorMessage(
            error,
            'Не удалось получить последние значения.'
          ));
        }
      });
  }
}
