import { TestBed } from '@angular/core/testing';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
  });

  it('starts empty', () => {
    expect(service.get()).toBeNull();
  });

  it('persists and reads a token', () => {
    service.set('abc');
    expect(service.get()).toBe('abc');
    expect(sessionStorage.getItem('ticketing.jwt')).toBe('abc');
  });

  it('clears the token', () => {
    service.set('abc');
    service.clear();
    expect(service.get()).toBeNull();
    expect(sessionStorage.getItem('ticketing.jwt')).toBeNull();
  });
});
