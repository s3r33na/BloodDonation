import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap, catchError, throwError, of } from 'rxjs';

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: string;
  eligibilityStatus: string;
  nationalId: string;
  nationality: string;
  mobileNumber?: string;
  dateOfBirth?: string;
  bloodType?: string;
  gender?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5007/api';

  // Signals for state management
  currentUser = signal<User | null>(null);
  token = signal<string | null>(null);
  notifications = signal<any[]>([]);
  unreadNotificationsCount = signal<number>(0);
  demoMode = signal(false);

  constructor() {
    const savedDemoMode = localStorage.getItem('bdms_demo_mode') === 'true';
    if (savedDemoMode) {
      this.enterDemoMode();
      return;
    }

    // Restore session on bootstrap
    const savedToken = localStorage.getItem('bdms_token');
    const savedUser = localStorage.getItem('bdms_user');
    if (savedToken && savedUser) {
      this.token.set(savedToken);
      this.currentUser.set(JSON.parse(savedUser));
      this.fetchNotifications().subscribe();
    }
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    if (this.token()) {
      headers = headers.set('Authorization', `Bearer ${this.token()}`);
    }
    return headers;
  }

  // Authentication APIs
  register(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register`, data).pipe(
      catchError(this.handleError)
    );
  }

  login(credentials: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/Auth/login`, credentials).pipe(
      tap(res => {
        if (res && res.token) {
          this.token.set(res.token);
          this.currentUser.set(res.user);
          localStorage.setItem('bdms_token', res.token);
          localStorage.setItem('bdms_user', JSON.stringify(res.user));
          this.fetchNotifications().subscribe();
        }
      }),
      catchError(this.handleError)
    );
  }

  isDemoMode() {
    return this.demoMode();
  }

  enterDemoMode() {
    const demoUser: User = {
      id: 999,
      fullName: 'Demo User',
      email: 'demo@blooddonation.app',
      role: 'Donor',
      eligibilityStatus: 'Eligible',
      nationalId: '000000000',
      nationality: 'Demo',
      mobileNumber: '+966500000000',
      dateOfBirth: '1990-01-01',
      bloodType: 'O+',
      gender: 'Prefer not to say'
    };

    this.demoMode.set(true);
    this.token.set('demo-token');
    this.currentUser.set(demoUser);
    this.notifications.set([
      { id: 1, title: 'Welcome to the demo dashboard', message: 'This preview uses sample data so you can explore the UI without the backend.', isRead: false },
      { id: 2, title: 'Demo appointment ready', message: 'You can review the booking layout and donation history cards.', isRead: true }
    ]);
    this.unreadNotificationsCount.set(1);
    localStorage.setItem('bdms_demo_mode', 'true');
    localStorage.setItem('bdms_token', 'demo-token');
    localStorage.setItem('bdms_user', JSON.stringify(demoUser));
  }

  logout() {
    this.demoMode.set(false);
    this.token.set(null);
    this.currentUser.set(null);
    this.notifications.set([]);
    this.unreadNotificationsCount.set(0);
    localStorage.removeItem('bdms_demo_mode');
    localStorage.removeItem('bdms_token');
    localStorage.removeItem('bdms_user');
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/Auth/me`, { headers: this.getHeaders() }).pipe(
      tap(user => {
        this.currentUser.set(user);
        localStorage.setItem('bdms_user', JSON.stringify(user));
      }),
      catchError(this.handleError)
    );
  }

  // Feed/Post APIs
  getFeed(filters: { type?: string; bloodType?: string; search?: string } = {}): Observable<any[]> {
    let params: any = {};
    if (filters.type) params.type = filters.type;
    if (filters.bloodType) params.bloodType = filters.bloodType;
    if (filters.search) params.search = filters.search;

    return this.http.get<any[]>(`${this.apiUrl}/Post`, { headers: this.getHeaders(), params }).pipe(
      catchError(this.handleError)
    );
  }

  createPost(postData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Post`, postData, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  updatePost(id: number, postData: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/Post/${id}`, postData, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  deletePost(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/Post/${id}`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  completePost(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/Post/${id}/complete`, {}, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  archivePost(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/Post/${id}/archive`, {}, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  // Donation Form APIs
  submitScreeningForm(formData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/DonationForm/submit`, formData, { headers: this.getHeaders() }).pipe(
      tap(() => this.getProfile().subscribe()), // update local profile status
      catchError(this.handleError)
    );
  }

  getDonationHistory(): Observable<any[]> {
    if (this.isDemoMode()) {
      return of([
        {
          id: 1,
          bloodType: 'O+',
          location: 'Riyadh Central Hospital',
          eligibilityResult: 'Eligible',
          checkedInAt: '2026-06-14T09:00:00'
        },
        {
          id: 2,
          bloodType: 'O+',
          location: 'King Abdulaziz Center',
          eligibilityResult: 'Eligible',
          checkedInAt: '2025-12-08T10:30:00'
        }
      ]);
    }

    return this.http.get<any[]>(`${this.apiUrl}/DonationForm/history`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  getAllDonationForms(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/DonationForm/all`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  reviewDonationForm(id: number, reviewData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/DonationForm/${id}/review`, reviewData, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  // Appointment APIs
  bookAppointment(payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointment/book`, payload, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  getMyAppointments(): Observable<any[]> {
    if (this.isDemoMode()) {
      return of([
        {
          id: 101,
          status: 'Booked',
          eventName: 'Community Blood Drive',
          eventLocation: 'Riyadh Central Hospital',
          appointmentDateTime: '2026-07-22T09:00:00',
          qrCodeToken: 'DEMO-QR-101'
        },
        {
          id: 102,
          status: 'CheckedIn',
          eventName: 'Weekend Donation Camp',
          eventLocation: 'King Abdulaziz Center',
          appointmentDateTime: '2026-07-25T14:30:00',
          qrCodeToken: 'DEMO-QR-102'
        }
      ]);
    }

    return this.http.get<any[]>(`${this.apiUrl}/Appointment/mine`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  cancelAppointment(id: number): Observable<any> {
    if (this.isDemoMode()) {
      return of({ success: true, id });
    }

    return this.http.post(`${this.apiUrl}/Appointment/${id}/cancel`, {}, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  getAllAppointments(search?: string, status?: string): Observable<any[]> {
    let params: any = {};
    if (search) params.search = search;
    if (status) params.status = status;
    return this.http.get<any[]>(`${this.apiUrl}/Appointment/all`, { headers: this.getHeaders(), params }).pipe(
      catchError(this.handleError)
    );
  }

  checkInAppointment(qrCodeToken: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointment/check-in`, { qrCodeToken }, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  updateAttendanceStatus(id: number, status: string, notes: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointment/${id}/attendance-status`, { status, notes }, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  resendAppointmentDetails(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/Appointment/${id}/resend`, {}, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  // Admin Dashboard Statistics APIs
  getDashboardStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/Dashboard/stats`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  getDashboardCharts(): Observable<any> {
    return this.http.get(`${this.apiUrl}/Dashboard/charts`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  getAdminUsersList(filters: { search?: string; eligibility?: string; bloodType?: string } = {}): Observable<any[]> {
    let params: any = {};
    if (filters.search) params.search = filters.search;
    if (filters.eligibility) params.eligibility = filters.eligibility;
    if (filters.bloodType) params.bloodType = filters.bloodType;

    return this.http.get<any[]>(`${this.apiUrl}/Dashboard/users`, { headers: this.getHeaders(), params }).pipe(
      catchError(this.handleError)
    );
  }

  editUserProfile(id: number, profileData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Dashboard/user/${id}/edit`, profileData, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError)
    );
  }

  // Notifications API
  fetchNotifications(): Observable<any[]> {
    if (this.isDemoMode()) {
      const demoNotifications = [
        { id: 1, title: 'Welcome to the demo dashboard', message: 'This preview uses sample data so you can explore the UI without the backend.', isRead: false },
        { id: 2, title: 'Demo appointment ready', message: 'You can review the booking layout and donation history cards.', isRead: true }
      ];
      this.notifications.set(demoNotifications);
      this.unreadNotificationsCount.set(demoNotifications.filter(n => !n.isRead).length);
      return of(demoNotifications);
    }

    return this.http.get<any[]>(`${this.apiUrl}/Dashboard/notifications`, { headers: this.getHeaders() }).pipe(
      tap(notes => {
        this.notifications.set(notes);
        this.unreadNotificationsCount.set(notes.filter(n => !n.isRead).length);
      }),
      catchError(this.handleError)
    );
  }

  markNotificationsRead(): Observable<any> {
    return this.http.post(`${this.apiUrl}/Dashboard/notifications/read`, {}, { headers: this.getHeaders() }).pipe(
      tap(() => {
        const updated = this.notifications().map(n => ({ ...n, isRead: true }));
        this.notifications.set(updated);
        this.unreadNotificationsCount.set(0);
      }),
      catchError(this.handleError)
    );
  }

  // Report Export API
  exportReport(type: string, format: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/Report/export`, {
      headers: this.getHeaders(),
      params: { type, format },
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  exportEventAnalysis(eventId: number, format: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/Report/export/event-analysis/${eventId}`, {
      headers: this.getHeaders(),
      params: { format },
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  exportEventAttendeeDetails(eventId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/Report/export/event-attendees/${eventId}`, {
      headers: this.getHeaders(),
      params: { format: 'excel' },
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: any) {
    let message = 'An unknown error occurred.';
    if (error.error && error.error.message) {
      message = error.error.message;
    } else if (error.message) {
      message = error.message;
    }
    return throwError(() => new Error(message));
  }
}
