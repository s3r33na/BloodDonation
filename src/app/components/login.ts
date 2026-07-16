import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { AppLogo } from './logo';
import { LanguageService } from '../services/language.service';
import { gsap } from 'gsap';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, AppLogo],
  templateUrl: './login.html'
})
export class Login implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  email = '';
  password = '';
  loading = signal(false);
  errorMsg = signal('');

  @ViewChild('loginCard', { static: true }) loginCard!: ElementRef;

  ngOnInit() {
    // GSAP intro animation
    gsap.fromTo(this.loginCard.nativeElement,
      { opacity: 0, y: 30, scale: 0.95 },
      { opacity: 1, y: 0, scale: 1, duration: 0.8, ease: 'power3.out' }
    );
  }

  onSubmit() {
    if (!this.email || !this.password) {
      this.errorMsg.set('Please fill in all fields.');
      return;
    }

    this.loading.set(true);
    this.errorMsg.set('');

    this.api.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.user.role === 'Admin') {
          this.router.navigate(['/admin']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Login failed. Invalid email or password.');
      }
    });
  }

  viewDemo() {
    this.api.enterDemoMode();
    this.router.navigate(['/dashboard']);
  }
}
