import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from './services/api.service';
import { AppLogo } from './components/logo';
import { LanguageService } from './services/language.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AppLogo, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('BloodDonation');

  api = inject(ApiService);
  router = inject(Router);
  lang = inject(LanguageService);

  user = this.api.currentUser;
  notifications = this.api.notifications;
  unreadCount = this.api.unreadNotificationsCount;

  showNotificationsDropdown = signal(false);
  darkMode = signal(false);

  ngOnInit() {
    // Force light mode
    this.darkMode.set(false);
    this.applyDarkMode(false);
  }

  toggleDarkMode() {
    // Disabled - light mode only
  }

  private applyDarkMode(isDark: boolean) {
    document.body.classList.remove('dark-mode');
    document.documentElement.classList.remove('dark');
  }

  toggleNotifications() {
    this.showNotificationsDropdown.update(v => !v);
    if (this.showNotificationsDropdown() && this.unreadCount() > 0) {
      // Mark all read when user clicks dropdown
      this.api.markNotificationsRead().subscribe();
    }
  }

  onLogout() {
    this.api.logout();
    this.showNotificationsDropdown.set(false);
    this.router.navigate(['/login']);
  }
}
