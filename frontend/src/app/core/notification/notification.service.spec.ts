import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  const snack = { open: vi.fn() };
  let service: NotificationService;

  beforeEach(() => {
    snack.open.mockReset();
    TestBed.configureTestingModule({
      providers: [NotificationService, { provide: MatSnackBar, useValue: snack }],
    });
    service = TestBed.inject(NotificationService);
  });

  it('opens a snackbar for errors', () => {
    service.error('boom');
    expect(snack.open).toHaveBeenCalledWith('boom', 'Dismiss', expect.any(Object));
  });

  it('opens a snackbar for success', () => {
    service.success('yay');
    expect(snack.open).toHaveBeenCalledWith('yay', 'Dismiss', expect.any(Object));
  });
});
