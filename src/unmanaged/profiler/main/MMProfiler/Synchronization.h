#pragma once

class CMutex
{
public:
    CMutex() : m_hMutex(NULL) {}
    ~CMutex() { if (m_hMutex!=NULL) { ::CloseHandle(m_hMutex); m_hMutex=NULL; } }

public:
    void Initialise(const TCHAR * pName) { m_hMutex = ::CreateMutex(NULL, false, pName); }
    void Enter(){ if (IsValid()) { ::WaitForSingleObject(m_hMutex, INFINITE);} }
    void Leave(){ if (IsValid()) { ::ReleaseMutex(m_hMutex);} }
    bool IsValid() { return m_hMutex!=NULL; }
private:
    HANDLE m_hMutex;
};

template<class T>
class CScopedLock
{
public:
    CScopedLock<T>(T&entity) : m_entity(entity) { m_entity.Enter(); }
    ~CScopedLock(void) { m_entity.Leave(); }
private:
    T &m_entity;
};

class CEvent
{
public:
    CEvent () : m_hEvent(NULL) { }
    ~CEvent() { if (m_hEvent!= NULL) { ::CloseHandle(m_hEvent); m_hEvent = NULL; } }

public:
    void Initialise(const TCHAR * pName) { m_hEvent = ::CreateEvent(NULL, true, false, pName); }
    void Set() { ::SetEvent(m_hEvent); }
    void Wait() { ::WaitForSingleObject(m_hEvent, INFINITE); }

    void Reset() { ::ResetEvent(m_hEvent); }
	void SignalAndWait(CEvent &waitEvent) {::SignalObjectAndWait(m_hEvent, waitEvent.m_hEvent, INFINITE, false);}
    bool IsValid() { return m_hEvent!=NULL; }

private:
    HANDLE m_hEvent;
};

