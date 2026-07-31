import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { ResultsSection } from './results-section';

describe('ResultsSection', () => {
  it('should load and render results', async () => {
    await TestBed.configureTestingModule({
      imports: [ResultsSection],
      providers: [
        {
          provide: ApiService,
          useValue: {
            getResults: () => of([
              {
                id: 7,
                fileName: 'Sample.csv',
                timeDeltaSeconds: 30,
                firstOperationDate: '2026-01-10T10:00:00Z',
                averageExecutionTime: 1.8,
                averageValue: 15.5,
                medianValue: 15.5,
                maximumValue: 20.5,
                minimumValue: 10.5
              }
            ])
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(ResultsSection);
    fixture.componentRef.setInput('refreshToken', 0);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Sample.csv');
  });
});
