import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InitiativeTracker } from './initiative-tracker';

describe('InitiativeTracker', () => {
  let component: InitiativeTracker;
  let fixture: ComponentFixture<InitiativeTracker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InitiativeTracker],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InitiativeTracker);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('assets/encounters/goblin-ambush.json').flush([]);
    http.verify();
  });
});
