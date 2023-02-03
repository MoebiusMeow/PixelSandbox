# PixelSandboxMod
泰拉瑞亚MOD：开源落沙模拟系统

![Image](https://github.com/MoebiusMeow/PixelSandbox/blob/main/icon_large.png)

+ 那堆dll是和XNBCompiler一起用，便于编译hlsl3.0的xnb文件用的

### 功能说明
+ 落沙模拟系统，模仿noita
+ 支持整个世界范围的任意量落沙（使用区块加载、卸载）
+ 支持可持久化（保存在World目录下的一个子目录下，每个chunk单独存了一个文件）
+ 支持与玩家碰撞，按住上下来走上走下沙堆
+ 自动作差生成法线图，计算光照特效
+ 使用GPU计算落沙，跑得很快

### 交互演示
+ 按T（丢弃）放置沙子（在Mod主类开启Debug模式）
+ 在Mod主类可开启区块边框显示
+ 挖掘任意物块时会产生一堆沙子