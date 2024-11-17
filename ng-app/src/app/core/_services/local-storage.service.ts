import { Injectable } from '@angular/core';

@Injectable()
export class LocalStorageService {
  constructor() {}

  private _store = window.localStorage;

  get(key: string): any {
    if (!localStorage) return null;
    const unParsedValue: string | null = this._store.getItem(key);
    if (unParsedValue) {
      if (unParsedValue === 'undefined') return null;
      // @ts-ignore
      return JSON.parse(this._store.getItem(key));
    }

    return unParsedValue;
  }

  add(key: string, data: any) {
    this._store.setItem(key, JSON.stringify(data));
  }

  save(key: string, data: any) {
    this._store.setItem(key, JSON.stringify(data));
  }

  remove(key: string) {
    this._store.removeItem(key);
  }
}
