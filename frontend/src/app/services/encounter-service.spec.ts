import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { EncounterService } from './encounter-service';

describe('EncounterService', () => {
  let service: EncounterService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EncounterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
