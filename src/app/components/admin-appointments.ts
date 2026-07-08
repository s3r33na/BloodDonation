import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-admin-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-appointments.html'
})
export class AdminAppointments implements OnInit {
  private api = inject(ApiService);
  protected readonly lang = inject(LanguageService);

  appointments = signal<any[]>([]);
  loading = signal(false);
  successMsg = signal('');

  searchQuery = '';
  statusFilter = '';

  ngOnInit() {
    this.loadAppointments();
  }

  loadAppointments() {
    this.loading.set(true);
    this.api.getAllAppointments(this.searchQuery, this.statusFilter).subscribe({
      next: (data) => {
        this.appointments.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  resetFilters() {
    this.searchQuery = '';
    this.statusFilter = '';
    this.loadAppointments();
  }

  cancelBooking(id: number) {
    if (confirm('Are you sure you want to cancel this booking? This will restore the event slot.')) {
      this.api.cancelAppointment(id).subscribe({
        next: () => {
          this.successMsg.set('Booking canceled successfully.');
          this.loadAppointments();
        }
      });
    }
  }

  resendDetails(id: number) {
    this.api.resendAppointmentDetails(id).subscribe({
      next: () => {
        this.successMsg.set('Appointment check-in details resent to donor notification tray.');
        setTimeout(() => this.successMsg.set(''), 3000);
      }
    });
  }
}
