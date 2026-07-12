import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-reports.html'
})
export class AdminReports implements OnInit {
  private api = inject(ApiService);
  protected readonly lang = inject(LanguageService);

  reportFormat = 'pdf';
  reportScope = signal<'analysis' | 'attendees'>('analysis');
  exporting = signal(false);
  events = signal<any[]>([]);
  selectedEventId = signal('');

  ngOnInit() {
    this.loadEvents();
  }

  private loadEvents() {
    this.api.getFeed({ type: 'Event' }).subscribe({
      next: (events) => {
        this.events.set(events || []);
        if (events?.length) {
          this.selectedEventId.set(String(events[0].id));
        }
      },
      error: () => {
        this.events.set([]);
        alert('Unable to load blood donation events for reporting.');
      }
    });
  }

  triggerDownload() {
    const eventId = Number(this.selectedEventId());
    if (!eventId) {
      alert('Please select a blood donation event first.');
      return;
    }

    this.exporting.set(true);

    const request$ = this.reportScope() === 'attendees'
      ? this.api.exportEventAttendeeDetails(eventId)
      : this.api.exportEventAnalysis(eventId, this.reportFormat);

    request$.subscribe({
      next: (blob) => {
        this.exporting.set(false);

        const format = this.reportScope() === 'attendees' ? 'excel' : this.reportFormat;
        let ext = format === 'excel' ? 'xls' : format;
        let mimeType = '';
        if (format === 'pdf') mimeType = 'application/pdf';
        else if (format === 'excel') mimeType = 'application/vnd.ms-excel';
        else mimeType = 'text/csv';

        const fileBlob = new Blob([blob], { type: mimeType });
        const url = window.URL.createObjectURL(fileBlob);

        const a = document.createElement('a');
        a.href = url;
        a.download = this.reportScope() === 'attendees'
          ? `event_attendees_${eventId}_${new Date().toISOString().slice(0, 10)}.${ext}`
          : `event_analysis_${eventId}_${new Date().toISOString().slice(0, 10)}.${ext}`;
        document.body.appendChild(a);
        a.click();

        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exporting.set(false);
        alert(err.message || 'Export failed. The requested report data might be unavailable or the service is offline.');
      }
    });
  }
}
