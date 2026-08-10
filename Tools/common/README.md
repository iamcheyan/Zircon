# 共享与构建工具

这里存放 WIL/ZL 解码库、WTL 转换器、版本标记和百科数据链构建脚本。

完整百科数据链从仓库根目录运行：

```bash
bash Tools/common/rebuild_all.sh
```

脚本会调用其他工具分类目录，并不会把大型客户端资源或生成缓存加入 Git。
