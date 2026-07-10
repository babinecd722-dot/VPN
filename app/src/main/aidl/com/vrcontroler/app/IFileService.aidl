package com.vrcontroler.app;

interface IFileService {
    void destroy() = 16777114;
    void exit() = 1;

    String listDir(String path) = 2;
    boolean deletePath(String path) = 3;
    boolean createDir(String path) = 4;
    boolean renamePath(String src, String dst) = 5;
    String copyPath(String src, String dstDir) = 6;
    String movePath(String src, String dstDir) = 7;
    String readTextFile(String path) = 8;
    String writeTextFile(String path, String content) = 9;
    String extractZip(String zipPath, String dstDir) = 10;
    String statPath(String path) = 11;
}
