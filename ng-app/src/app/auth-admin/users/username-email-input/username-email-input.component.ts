import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

export class UsernameEmailInputData {
  valid: boolean;
  emailAddress: string;
}

@Component({
  selector: 'app-username-email-input',
  templateUrl: './username-email-input.component.html',
  styleUrls: ['./username-email-input.component.scss'],
})
export class UsernameEmailInputComponent implements OnInit {
  @Input() emailAddress: string;
  @Input() disabled: boolean;
  @Input() available = true;
  @Input() checked = false;
  @Input() checking = true;
  @Input() valid = false;
  @Input() labelText = 'Email';
  @Output() emailAddressUpdated = new EventEmitter<string>();

  constructor() {}

  ngOnInit() {}

  onEmailAddressChanged(event) {
    this.emailAddressUpdated.emit(event.target.value);
  }
}
