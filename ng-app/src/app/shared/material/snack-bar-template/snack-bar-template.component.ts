import { Component, OnInit, Input } from '@angular/core';
import { Inject } from '@angular/core';
import { MAT_SNACK_BAR_DATA } from '@angular/material/snack-bar';
import { LoggerModel } from '../../../core/_services/logger.service';

@Component({
    selector: 'app-snack-bar-template',
    templateUrl: './snack-bar-template.component.html',
    styleUrls: ['./snack-bar-template.component.scss'],
    standalone: false
})
export class SnackBarTemplateComponent implements OnInit {
  constructor(@Inject(MAT_SNACK_BAR_DATA) public data: LoggerModel) {}

  ngOnInit() {}
}
