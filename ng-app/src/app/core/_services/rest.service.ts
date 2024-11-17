import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfig } from '../../_api';
import { APP_CONFIG } from './config-injection';

@Injectable()
export class RestService<T = any> {
  constructor(
    private http: HttpClient,
    @Inject(APP_CONFIG) public config: AppConfig
  ) {}

  getResource(
    resourceName: string,
    resourceId: any = null,
    additionalQueryStringParameters: string = ''
  ): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    let url = this.getApiUrl() + resourceName;
    if (resourceId) url += '/' + encodeURIComponent(resourceId);

    if (additionalQueryStringParameters) {
      if (additionalQueryStringParameters.indexOf('?') === -1) url += '?';
      url += additionalQueryStringParameters;
    }

    return this.http.get(url, {
      headers: header,
      withCredentials: true,
    });
  }

  saveResource(resourceName: string, resource: T): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    let url = this.getApiUrl() + resourceName;
    const res = resource as any;

    if (res.id) {
      url += '/' + res.id;
      return this.http.put(url, resource, {
        headers: header,
        withCredentials: true,
      });
    } else {
      return this.http.post(url, resource, {
        headers: header,
        withCredentials: true,
      });
    }
  }

  deleteResource(resourceName: string, resourceId: any): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    let url = this.getApiUrl() + resourceName;
    if (resourceId !== null) url += '/' + resourceId;
    return this.http.delete(url, {
      headers: header,
      withCredentials: true,
    });
  }

  postResource<T = any>(resourceName: string, resource: any): Observable<T> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    const url = this.getApiUrl() + resourceName;
    return this.http.post<T>(url, resource, {
      headers: header,
      withCredentials: true,
    });
  }

  putResource(resourceName: string, resource: any): Observable<any> {
    const header = new HttpHeaders().set('Content-type', 'application/json');
    let url = this.getApiUrl() + resourceName;
    if (resource.id) url += '/' + resource.id;
    return this.http.put(url, resource, {
      headers: header,
      withCredentials: true,
    });
  }

  getApiUrl(): string {
    return this.config.environment.api;
  }
}
