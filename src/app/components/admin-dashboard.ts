import { Component, OnInit, OnDestroy, ViewChild, ElementRef, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';
import { Chart } from 'chart.js/auto';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-dashboard.html'
})
export class AdminDashboard implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  activeTab = 'overview';

  stats = signal<any | null>(null);
  statsLoading = signal(false);

  // Active bookings for simulations
  activeBookings = signal<any[]>([]);

  // QR check-in vars
  qrTokenInput = '';
  checkinLoading = signal(false);
  checkinSuccess = signal(false);
  checkinError = signal('');
  checkinResult = signal<any | null>(null);

  // Attendance queue vars
  attendanceQueue = signal<any[]>([]);

  // Status Change Modal State
  showModal = signal(false);
  targetPerson = signal<any | null>(null);
  newQueueStatus = 'Waiting';
  queueNotes = '';

  // Charts
  @ViewChild('bloodChart') bloodChartCanvas!: ElementRef;
  @ViewChild('attendanceChart') attendanceChartCanvas!: ElementRef;
  
  bloodChartRef: any;
  attendanceChartRef: any;

  constructor() {
    // Re-render charts when switching tab
    effect(() => {
      if (this.activeTab === 'charts') {
        setTimeout(() => this.renderCharts(), 200);
      }
    });
  }

  ngOnInit() {
    if (this.api.currentUser()?.role !== 'Admin') {
      this.router.navigate(['/dashboard']);
      return;
    }

    this.loadStats();
    this.loadActiveBookings();
    this.loadAttendanceList();
  }

  ngOnDestroy() {
    this.destroyCharts();
  }

  loadStats() {
    this.statsLoading.set(true);
    this.api.getDashboardStats().subscribe({
      next: (data) => {
        this.stats.set(data);
        this.statsLoading.set(false);
      },
      error: () => this.statsLoading.set(false)
    });
  }

  loadActiveBookings() {
    this.api.getAllAppointments(undefined, 'Booked').subscribe({
      next: (data) => this.activeBookings.set(data)
    });
  }

  loadAttendanceList() {
    this.api.getAllAppointments().subscribe({
      next: (appointments) => {
        // Attendance entries are those where status is CheckedIn or Completed/Rejected and are active in queue
        // We can fetch attendance details or filter from appointments
        const checkedInOrCompleted = appointments.filter(a => a.status === 'CheckedIn' || a.status === 'Completed');
        
        // Map to display structure
        const queue = checkedInOrCompleted.map((a: any) => ({
          id: a.id,
          checkInTime: a.checkedInAt || a.createdAt,
          donorName: a.donorName,
          donorNationalId: a.donorNationalId,
          donorBloodType: a.donorBloodType,
          eventName: a.eventName,
          status: a.status === 'CheckedIn' ? 'Waiting' : a.status // Check state inside queue
        }));
        
        // Since backend marks appointment status as CheckedIn and attendance status as Waiting,
        // let's poll actual attendance table records or simulate standard mapping
        this.attendanceQueue.set(queue);
      }
    });
  }

  onSimulateSelect(event: any) {
    const val = event.target.value;
    if (val) {
      this.qrTokenInput = val;
    }
  }

  onCheckInSubmit() {
    this.checkinLoading.set(true);
    this.checkinSuccess.set(false);
    this.checkinError.set('');

    this.api.checkInAppointment(this.qrTokenInput).subscribe({
      next: (res) => {
        this.checkinLoading.set(false);
        this.checkinSuccess.set(true);
        this.checkinResult.set(res);
        this.qrTokenInput = '';
        this.loadStats();
        this.loadActiveBookings();
        this.loadAttendanceList();
        this.api.fetchNotifications().subscribe();
      },
      error: (err) => {
        this.checkinLoading.set(false);
        this.checkinError.set(err.message || 'QR Verification failed. Code might be invalid or duplicate.');
      }
    });
  }

  openStatusModal(person: any) {
    this.targetPerson.set(person);
    // Find matching status in queue
    this.newQueueStatus = person.status;
    this.queueNotes = '';
    this.showModal.set(true);
  }

  closeStatusModal() {
    this.showModal.set(false);
  }

  submitStatusUpdate() {
    const id = this.targetPerson().id;
    this.api.updateAttendanceStatus(id, this.newQueueStatus, this.queueNotes).subscribe({
      next: () => {
        this.closeStatusModal();
        this.loadAttendanceList();
        this.loadStats();
        this.api.fetchNotifications().subscribe();
      },
      error: (err) => alert(err.message || 'Failed to update attendance status.')
    });
  }

  printDonorLabel(person: any) {
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      printWindow.document.write(`
        <html>
          <head>
            <title>Luminus Giving - Donor Label</title>
            <style>
              body { font-family: 'Courier New', monospace; padding: 20px; color: #000; }
              .label-card { border: 2px solid #000; padding: 15px; border-radius: 10px; width: 300px; }
              .title { font-size: 14pt; font-weight: bold; border-bottom: 2px solid #000; padding-bottom: 5px; margin-bottom: 10px; text-transform: uppercase; }
              .blood-type { font-size: 32pt; font-weight: bold; float: right; border: 3px solid #000; padding: 0 10px; line-height: 1; }
              .item { font-size: 10pt; margin-bottom: 5px; }
              .item strong { text-transform: uppercase; }
            </style>
          </head>
          <body onload="window.print();window.close();">
            <div class="label-card">
              <div class="blood-type">${person.donorBloodType}</div>
              <div class="title">Luminus Check-In</div>
              <div class="item"><strong>Donor:</strong> ${person.donorName}</div>
              <div class="item"><strong>ID:</strong> ${person.donorNationalId}</div>
              <div class="item"><strong>Event:</strong> ${person.eventName}</div>
              <div class="item"><strong>Check-In:</strong> ${new Date(person.checkInTime).toLocaleString()}</div>
              <div class="item"><strong>Status:</strong> ${person.status}</div>
            </div>
          </body>
        </html>
      `);
      printWindow.document.close();
    }
  }

  renderCharts() {
    this.destroyCharts();

    this.api.getDashboardCharts().subscribe({
      next: (data) => {
        const bloodLabels = data.bloodAvailability.map((x: any) => x.bloodType);
        const availabilityData = data.bloodAvailability.map((x: any) => x.count);
        const demandData = data.bloodDemand.map((x: any) => x.count);

        // Render supply vs demand bar chart
        if (this.bloodChartCanvas) {
          this.bloodChartRef = new Chart(this.bloodChartCanvas.nativeElement, {
            type: 'bar',
            data: {
              labels: bloodLabels,
              datasets: [
                {
                  label: 'Availability (Donors Supply)',
                  data: availabilityData,
                  backgroundColor: 'rgba(16, 185, 129, 0.7)',
                  borderColor: '#10B981',
                  borderWidth: 1
                },
                {
                  label: 'Demand (Urgent Required)',
                  data: demandData,
                  backgroundColor: 'rgba(211, 47, 47, 0.7)',
                  borderColor: '#D32F2F',
                  borderWidth: 1
                }
              ]
            },
            options: {
              responsive: true,
              maintainAspectRatio: false,
              scales: {
                y: { beginAtZero: true }
              }
            }
          });
        }

        // Render attendance rate doughnut chart
        if (this.attendanceChartCanvas) {
          this.attendanceChartRef = new Chart(this.attendanceChartCanvas.nativeElement, {
            type: 'doughnut',
            data: {
              labels: ['Checked In (Attended)', 'No-Show / Pending', 'Canceled bookings (excluded)'],
              datasets: [{
                data: [data.attendanceRate, data.noShowRate, Math.max(0, 100 - data.attendanceRate - data.noShowRate)],
                backgroundColor: [
                  '#10B981',
                  '#EF4444',
                  '#E2E8F0'
                ]
              }]
            },
            options: {
              responsive: true,
              maintainAspectRatio: false,
              plugins: {
                legend: { position: 'bottom' }
              }
            }
          });
        }
      }
    });
  }

  destroyCharts() {
    if (this.bloodChartRef) {
      this.bloodChartRef.destroy();
    }
    if (this.attendanceChartRef) {
      this.attendanceChartRef.destroy();
    }
  }
}
