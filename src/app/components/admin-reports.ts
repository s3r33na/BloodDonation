import { Component, inject, signal } from '@angular/core';
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
export class AdminReports {
  private api = inject(ApiService);
  protected readonly lang = inject(LanguageService);

  reportType = 'users';
  reportFormat = 'pdf';
  exporting = signal(false);

  triggerDownload() {
    this.exporting.set(true);

    this.api.exportReport(this.reportType, this.reportFormat).subscribe({
      next: (blob) => {
        this.exporting.set(false);

        // Determine extension
        let ext = this.reportFormat === 'excel' ? 'xls' : this.reportFormat;
        let mimeType = '';
        if (this.reportFormat === 'pdf') mimeType = 'application/pdf';
        else if (this.reportFormat === 'excel') mimeType = 'application/vnd.ms-excel';
        else mimeType = 'text/csv';

        const fileBlob = new Blob([blob], { type: mimeType });
        const url = window.URL.createObjectURL(fileBlob);
        
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.reportType}_report_${new Date().toISOString().slice(0,10)}.${ext}`;
        document.body.appendChild(a);
        a.click();
        
        // Cleanup
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exporting.set(false);
        alert(err.message || 'Export failed. Database table might be empty or service is offline.');
      }
    });
  }
}
