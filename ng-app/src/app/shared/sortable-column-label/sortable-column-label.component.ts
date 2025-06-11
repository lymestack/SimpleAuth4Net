import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-sortable-column-label',
  templateUrl: './sortable-column-label.component.html',
  styleUrls: ['./sortable-column-label.component.scss'],
})
export class SortableColumnLabelComponent implements OnInit {
  @Input() fieldName: string;
  @Input() sortDirection: string;
  @Input() sortField: string;
  @Output() sort = new EventEmitter<string>();

  constructor() {}

  ngOnInit() {}
}
