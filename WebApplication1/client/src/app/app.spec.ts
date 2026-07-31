import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { App } from './app';
import { ApiService } from './core/services/api.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: ApiService,
          useValue: {
            uploadCsv: () => undefined,
            getResults: () => of([]),
            getLatestValues: () => of([])
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render upload and results sections', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('h1')?.textContent).toContain('Загрузите CSV');
    expect(compiled.querySelector('#results-title')?.textContent)
      .toContain('Результаты обработки');
    expect(compiled.querySelector('#values-title')?.textContent)
      .toContain('Последние 10 значений');
  });
});
