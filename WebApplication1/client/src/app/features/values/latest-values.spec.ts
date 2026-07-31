import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { LatestValues } from './latest-values';

describe('LatestValues', () => {
  it('should load values for the provided file name', async () => {
    await TestBed.configureTestingModule({
      imports: [LatestValues],
      providers: [
        {
          provide: ApiService,
          useValue: {
            getLatestValues: () => of([
              {
                id: 1,
                date: '2026-01-10T10:00:30Z',
                executionTime: 2.4,
                value: 20.5
              }
            ])
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LatestValues);
    fixture.componentRef.setInput('initialFileName', 'Sample.csv');
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('20.5');
  });
});
