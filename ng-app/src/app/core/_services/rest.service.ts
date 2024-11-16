import { Inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class RestService<T = any> {
  constructor(private http: HttpClient) {}

  getResource(
    resourceName: string,
    resourceId: any = null,
    additionalQueryStringParameters: string = ''
  ): Observable<any> {
    let url = this.getApiUrl() + resourceName;
    if (resourceId) url += '/' + encodeURIComponent(resourceId);

    if (additionalQueryStringParameters) {
      if (additionalQueryStringParameters.indexOf('?') === -1) url += '?';
      url += additionalQueryStringParameters;
    }
    debugger;
    return this.http.get(url);
  }

  saveResource(resourceName: string, resource: T): Observable<any> {
    let url = this.getApiUrl() + resourceName;
    const res = resource as any;

    if (res.id) {
      url += '/' + res.id;
      return this.http.put(url, resource);
    } else {
      return this.http.post(url, resource);
    }
  }

  deleteResource(resourceName: string, resourceId: any): Observable<any> {
    let url = this.getApiUrl() + resourceName;
    if (resourceId !== null) url += '/' + resourceId;
    return this.http.delete(url);
  }

  postResource<T = any>(resourceName: string, resource: any): Observable<T> {
    const url = this.getApiUrl() + resourceName;
    return this.http.post<T>(url, resource);
  }

  putResource(resourceName: string, resource: any): Observable<any> {
    let url = this.getApiUrl() + resourceName;
    if (resource.id) url += '/' + resource.id;
    return this.http.put(url, resource);
  }

  getApiUrl(): string {
    // return this.config.environment.api;
    return 'http://localhost:7214/';
  }
}
