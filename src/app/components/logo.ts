import { Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  templateUrl: './logo.html'
})
export class AppLogo {
  size = input<number>(48);
  showText = input<boolean>(true);
}
