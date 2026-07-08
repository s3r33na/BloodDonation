import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { AppLogo } from './logo';
import { LanguageService } from '../services/language.service';
import { gsap } from 'gsap';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, AppLogo],
  templateUrl: './register.html'
})
export class Register implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  fullName = '';
  nationality = 'Jordanian';
  nationalId = '';
  mobileNumber = '';
  dateOfBirth = '';
  gender = 'Male';
  email = '';
  password = '';

  loading = signal(false);
  errorMsg = signal('');
  successMsg = signal('');

  @ViewChild('registerCard', { static: true }) registerCard!: ElementRef;

  ngOnInit() {
    gsap.fromTo(this.registerCard.nativeElement,
      { opacity: 0, y: 30, scale: 0.95 },
      { opacity: 1, y: 0, scale: 1, duration: 0.8, ease: 'power3.out' }
    );
  }

  onNationalityChange() {
    if (this.nationality !== 'Jordanian') {
      this.nationalId = '';
    }
  }

  onSubmit() {
    // Client-side validations
    if (!this.fullName || !this.mobileNumber || !this.dateOfBirth || !this.email || !this.password) {
      this.errorMsg.set('Please fill in all required fields.');
      return;
    }

    if (this.nationality === 'Jordanian') {
      if (!this.nationalId || !/^\d{10}$/.test(this.nationalId)) {
        this.errorMsg.set('Jordanian National ID must be exactly 10 numeric digits.');
        return;
      }
    }

    if (this.password.length < 6) {
      this.errorMsg.set('Password must be at least 6 characters long.');
      return;
    }

    this.loading.set(true);
    this.errorMsg.set('');
    this.successMsg.set('');

    const payload = {
      fullName: this.fullName,
      nationality: this.nationality,
      nationalId: this.nationality === 'Jordanian' ? this.nationalId : 'N/A',
      mobileNumber: this.mobileNumber,
      dateOfBirth: this.dateOfBirth,
      gender: this.gender,
      email: this.email,
      password: this.password
    };

    this.api.register(payload).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMsg.set('Registration successful! Redirecting to login page...');
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Registration failed. Email or National ID might be in use.');
      }
    });
  }
}
