# Open Host Service（OHS）  
  
在 NCF 系统中，我们采用了领域驱动设计（DDD）中的Open Host Service（OHS）模式，以实现系统内部模块或领域之间，以及与外部系统之间的通信。OHS通过公开的、统一的接口（如API、事件等）来实现解耦和易于集成的通信。OHS分为两个分支：[Local](Local/readme.md) 和 [Remote](Remote/readme.md)。 