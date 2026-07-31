import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, Input, OnChanges, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import {
  ResultFilters,
  ResultResponse
} from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { getHttpErrorMessage } from '../../core/utils/http-error';

@Component({
  selector: 'app-results-section',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  templateUrl: './results-section.html',
  styleUrl: './results-section.scss'
})
export class ResultsSection implements OnChanges {
  private readonly api = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);

  @Input() refreshToken = 0;

  protected readonly results = signal<ResultResponse[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly filterForm = this.formBuilder.group({
    fileName: [''],
    firstOperationDateFrom: [''],
    firstOperationDateTo: [''],
    averageValueFrom: [null as number | null],
    averageValueTo: [null as number | null],
    averageExecutionTimeFrom: [null as number | null],
    averageExecutionTimeTo: [null as number | null]
  });

  ngOnChanges(): void {
    this.loadResults();
  }

  protected applyFilters(event: Event): void {
    event.preventDefault();
    this.loadResults();
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      fileName: '',
      firstOperationDateFrom: '',
      firstOperationDateTo: '',
      averageValueFrom: null,
      averageValueTo: null,
      averageExecutionTimeFrom: null,
      averageExecutionTimeTo: null
    });
    this.loadResults();
  }

  protected loadResults(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    this.isLoading.set(true);

    this.api.getResults(this.buildFilters())
      .pipe(finalize(() => {
        this.isLoading.set(false);
        this.isLoaded.set(true);
      }))
      .subscribe({
        next: (results) => this.results.set(results),
        error: (error: HttpErrorResponse) => {
          this.results.set([]);
          this.errorMessage.set(getHttpErrorMessage(
            error,
            'Не удалось получить сохранённые результаты.'
          ));
        }
      });
  }

  private buildFilters(): ResultFilters {
    const value = this.filterForm.getRawValue();

    return {
      fileName: value.fileName?.trim() || undefined,
      firstOperationDateFrom: this.toIsoString(value.firstOperationDateFrom),
      firstOperationDateTo: this.toIsoString(value.firstOperationDateTo),
      averageValueFrom: value.averageValueFrom ?? undefined,
      averageValueTo: value.averageValueTo ?? undefined,
      averageExecutionTimeFrom: value.averageExecutionTimeFrom ?? undefined,
      averageExecutionTimeTo: value.averageExecutionTimeTo ?? undefined
    };
  }

  private toIsoString(value: string | null | undefined): string | undefined {
    if (!value) {
      return undefined;
    }

    return new Date(value).toISOString();
  }
}
