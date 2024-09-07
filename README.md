# dotmakeup

[![builds.sr.ht status](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml.svg)](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml?)

## About

dotmakeup is a way to follow users of closed social networks (such as Twitter, Instagram and more) from any ActivityPub service. The aim is to make their posts appear as native a possible to the fediverse, while being as scalable as possible. 

## Supported networks

| Network   | Features                 |
|-----------|--------------------------|
| Twitter   | Posts, Retweets, Replies |
| Instagram | Posts                    |


Modes are upcoming for Reddit, TikTok and HN.

## Official instances

- [bird.makeup](https://bird.makeup) for dotmakeup in Twitter mode
- [kilogram.makeup](https://kilogram.makeup) for dotmakeup in Instagram mode


If you are an instance admin that prefers to not have tweets and/or instagram posts federated to you, please block the entire instance. 

Please consider if you really need another instance before spinning up a new one, as having multiple domain makes it harder for moderators to block twitter content. 

### Compared to BirdsiteLive, bird.makeup is:

More scalable:
- Twitter API calls are not rate-limited
- It is possible to split the Twitter crawling to multiple servers
- There are now integration tests for the non-official api
- The core pipeline has been tweaked to remove bottlenecks. As of writing this, bird.makeup supports without problems more than 90k users.

More native to the fediverse:
- Retweets are propagated as boosts
- Activities are now "unlisted" which means that they won't polute the public timeline, but they can still be boosted
- WIP support for QT

## License

Original code started from [BirdsiteLive](https://github.com/NicolasConstant/BirdsiteLive), but has now been changed significantly. BirdsiteLive is not maintained anymore.

This project is licensed under the AGPLv3 License - see [LICENSE](https://git.sr.ht/~cloutier/bird.makeup/tree/master/item/LICENSE) for details.

## Contact

You can contact me via ActivityPub <a rel="me" href="https://social.librem.one/@vincent">here</a>.


