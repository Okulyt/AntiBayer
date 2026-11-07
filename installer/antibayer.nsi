; AntiBayer installer (NSIS 2.46).
; Build through ..\build-installer.bat: it publishes the self-contained app into ..\publish and
; passes the version read from the published exe.

!include "MUI2.nsh"

!define PRODUCT_NAME "AntiBayer"
!define EXE_NAME "AntiBayer.exe"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AntiBayer"

!ifndef PRODUCT_VERSION
  !error "PRODUCT_VERSION is missing. Please build through build-installer.bat."
!endif

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "AntiBayer-Setup.exe"
InstallDir "$PROGRAMFILES64\AntiBayer"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXE_NAME}"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE.txt"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Install"
  ; 64-bit build: use the native registry view, not the WOW6432Node one of a 32-bit installer.
  SetRegView 64

  ; An update replaces the whole folder, so that no files of an older version stay behind.
  ; The folder is fixed on purpose (no directory page): RMDir /r here and in the uninstaller
  ; must only ever hit our own folder, never one the user picked and that holds other files.
  IfFileExists "$INSTDIR\${EXE_NAME}" 0 +2
    RMDir /r "$INSTDIR"

  SetOutPath "$INSTDIR"
  File /r "..\publish\*.*"
  File "..\LICENSE.txt"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  SetShellVarContext all
  CreateShortCut "$SMPROGRAMS\AntiBayer.lnk" "$INSTDIR\${EXE_NAME}"

  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "Sven Wienstein"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\${EXE_NAME}"
  WriteRegStr HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetRegView 64
  SetShellVarContext all
  Delete "$SMPROGRAMS\AntiBayer.lnk"
  DeleteRegKey HKLM "${UNINST_KEY}"
  RMDir /r "$INSTDIR"
SectionEnd
