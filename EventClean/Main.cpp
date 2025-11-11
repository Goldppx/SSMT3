#include <windows.h>
#include <iostream>
#include <string>

int wmain(int argc, wchar_t* argv[])
{
    // 1. 打开“Application”日志
    HANDLE hLog = OpenEventLogW(nullptr, L"Application");
    if (hLog == nullptr)
    {
        std::wcout << L"OpenEventLog 失败，错误码: " << GetLastError() << std::endl;
        getchar();
        return 1;
    }

    // 2. 直接清空
    if (!ClearEventLogW(hLog, nullptr))   // 第二个参数是备份文件名，nullptr=不备份
    {
        std::wcout << L"ClearEventLog 失败，错误码: " << GetLastError() << std::endl;
        CloseEventLog(hLog);
        getchar();
        return 2;
    }

    std::wcout << L"Application 日志已清空。" << std::endl;
    CloseEventLog(hLog);

    // ===== 清理完成，等待用户 =====
    std::wcout << L"\n所有日志清理完成，按任意键退出...";
    getchar();
    return 0;
}