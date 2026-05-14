import { HttpError, ValidationError, type ValidationErrorModel } from "./customErrors";

function checkResponseForErrors(response: Response, json: any): void{
  if (!response.ok && isValidationError(json)){
    throw new ValidationError(json);
  }

  if (!response.ok) {
    throw new HttpError(json);
  }
}

function isValidationError(data: any): data is ValidationErrorModel {
  return data && typeof data.errorStates === 'object' && !Array.isArray(data.errors);
}

export async function get<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: 'GET'
  });
  const cloned = response.clone();
  const text = await response.text();

  if (!text || text.length == 0) {
    console.log("undef")
    return Promise.resolve(undefined as T);
  }

  const json = await cloned.json();
  checkResponseForErrors(response, json);
  return json; // This is the "mapping"
}

export async function del<T>(url: string, bodyContent: any|undefined): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: 'DELETE',
    body: bodyContent ? JSON.stringify(bodyContent) : '',
        headers:{
      'Content-Type':'application/json'
    }
  });
  const cloned = response.clone();
  const text = await response.text();

  if (!text || text.length == 0) {
    return Promise.resolve(undefined as T);
  }
  const json = await cloned.json();
  checkResponseForErrors(response, json);
  return json; // This is the "mapping"
}

export async function post<T>(url: string, bodyContent: any | undefined): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'POST',
    body: bodyContent ? JSON.stringify(bodyContent) : '',
    headers:{
      'Content-Type':'application/json'
    }
  });
  const cloned = response.clone();
  const text = await response.text();

  if (!text || text.length == 0) {
    return Promise.resolve(undefined as T);
  }
  const json = await cloned.json();
  checkResponseForErrors(response, json);
  return json; // This is the "mapping"
}

export async function patch<T>(url: string, bodyContent: any|undefined): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'PATCH',
    body: bodyContent ? JSON.stringify(bodyContent) : '',
        headers:{
      'Content-Type':'application/json'
    }
  });
  const cloned = response.clone();
  const text = await response.text();

  if (!text || text.length == 0) {
    return Promise.resolve(undefined as T);
  }
  const json = await cloned.json();
  checkResponseForErrors(response, json);
  return json; // This is the "mapping"
}

export async function put<T>(url: string, bodyContent: any|undefined): Promise<T>{
  const response = await fetch(url, {
    credentials: "include",
    method: 'PUT',
    body: bodyContent ? JSON.stringify(bodyContent) : '',
        headers:{
      'Content-Type':'application/json'
    }
  });
  const cloned = response.clone();
  const text = await response.text();

  if (!text || text.length == 0) {
    return Promise.resolve(undefined as T);
  }
  const json = await cloned.json();
  checkResponseForErrors(response, json);
  return json; // This is the "mapping"
}

