import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ApiService } from './api.service';

describe('ApiService demo mode', () => {
  let service: ApiService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(ApiService);
  });

  it('should activate demo mode with preview user data', () => {
    service.enterDemoMode();

    expect(service.isDemoMode()).toBeTrue();
    expect(service.currentUser()?.fullName).toBe('Demo User');
    expect(service.currentUser()?.role).toBe('Donor');
  });
});
