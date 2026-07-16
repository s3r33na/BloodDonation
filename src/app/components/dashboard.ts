import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ApiService, User } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html'
})
export class Dashboard implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  user = this.api.currentUser;
  
  upcomingAppointments = signal<any[]>([]);
  donationHistory = signal<any[]>([]);
  activeAppt = signal<any | null>(null);

  appointmentsLoading = signal(false);
  historyLoading = signal(false);

  completedDonationsCount = signal<number>(0);
  bloodType = signal<string>('Pending Screening');

  ngOnInit() {
    if (!this.user()) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadAppointments();
    this.loadHistory();
  }

  loadAppointments() {
    this.appointmentsLoading.set(true);
    this.api.getMyAppointments().pipe(
      // filter out canceled appointments
    ).subscribe({
      next: (data) => {
        const active = data.filter(a => a.status === 'Booked' || a.status === 'CheckedIn');
        this.upcomingAppointments.set(active);
        
        // Auto select first booking to show QR code
        if (active.length > 0) {
          this.activeAppt.set(active[0]);
        } else {
          this.activeAppt.set(null);
        }
        
        this.appointmentsLoading.set(false);
      },
      error: () => this.appointmentsLoading.set(false)
    });
  }

  loadHistory() {
    this.historyLoading.set(true);
    this.api.getDonationHistory().subscribe({
      next: (data) => {
        const history = Array.isArray(data) ? data : [];
        this.donationHistory.set(history);
        if (history.length > 0) {
          const latest = history[0];
          const bloodGroup = latest?.bloodGroup || latest?.bloodType?.[0] || 'O';
          const rhFactor = latest?.rhFactor || (latest?.bloodType?.includes('-') ? '-' : '+');
          this.bloodType.set(`${bloodGroup}${rhFactor}`);
          this.completedDonationsCount.set(history.filter(x => x?.eligibilityResult === 'Eligible').length);
        } else {
          this.bloodType.set('Pending Screening');
          this.completedDonationsCount.set(0);
        }
        this.historyLoading.set(false);
      },
      error: () => {
        this.historyLoading.set(false);
        this.bloodType.set('Pending Screening');
        this.completedDonationsCount.set(0);
      }
    });
  }

  selectAppointment(appt: any) {
    this.activeAppt.set(appt);
  }

  cancelAppointment(id: number) {
    if (confirm('Are you sure you want to cancel this booking?')) {
      this.api.cancelAppointment(id).subscribe({
        next: () => {
          this.loadAppointments();
          this.api.fetchNotifications().subscribe();
        },
        error: (err) => alert(err.message || 'Failed to cancel appointment.')
      });
    }
  }

  getEligibilityBadgeClass() {
    const status = this.user()?.eligibilityStatus;
    if (status === 'Eligible') return 'bg-emerald-50  text-emerald-600 ';
    if (status === 'TemporarilyNotEligible') return 'bg-amber-50  text-amber-600 ';
    return 'bg-red-50  text-brand-red';
  }

  getBloodGroupDisplay() {
    return this.bloodType();
  }

  getEligibilityHelperText() {
    const status = this.user()?.eligibilityStatus;
    if (status === 'Eligible') return 'You are cleared to book donation events.';
    if (status === 'TemporarilyNotEligible') return 'Suspended temporarily. Check expiry or resubmit form.';
    if (status === 'PermanentlyNotEligible') return 'Permanently ineligible due to regulatory guidelines.';
    return 'Registration approved. Please complete screening form.';
  }
}
